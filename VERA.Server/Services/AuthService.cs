using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VERA.Server.Data;
using VERA.Shared;
using VERA.Shared.Dto;

namespace VERA.Server.Services
{
    public class AuthService
    {
        private const int SaltSize          = 32;
        private const int HashSize          = 32;
        private const int Pbkdf2Iterations  = 300_000;
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration     = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan AccessTokenLifetime  = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

        private readonly VeraDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(VeraDbContext db, IConfiguration config)
        {
            _db     = db;
            _config = config;
        }

        // ── Registrierung ──────────────────────────────────────────────────────
        public async Task<(bool Ok, string Error)> RegisterAsync(string username, string password)
        {
            username = username.Trim();
            if (username.Length < 2 || username.Length > 50)
                return (false, "Benutzername muss 2–50 Zeichen lang sein.");
            if (password.Length < 8)
                return (false, "Passwort muss mindestens 8 Zeichen lang sein.");
            if (await _db.Users.AnyAsync(u => u.Username == username))
                return (false, "Benutzername bereits vergeben.");

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Pbkdf2(password, salt);

            _db.Users.Add(new User
            {
                Username     = username,
                PasswordSalt = Convert.ToBase64String(salt),
                PasswordHash = Convert.ToBase64String(hash),
                CreatedAt    = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();
            return (true, string.Empty);
        }

        // ── Login ──────────────────────────────────────────────────────────────
        public async Task<(LoginResult Result, AuthResponse? Response)> LoginAsync(
            string username, string password, string clientIp)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username.Trim());
            if (user is null) return (LoginResult.NoAccountFound, null);

            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
                return (LoginResult.AccountLocked, null);

            // Alten Lockout aufheben
            if (user.LockedUntil.HasValue)
            {
                user.LockedUntil    = null;
                user.FailedAttempts = 0;
            }

            var salt         = Convert.FromBase64String(user.PasswordSalt);
            var expectedHash = Convert.FromBase64String(user.PasswordHash);
            var actualHash   = Pbkdf2(password, salt);

            if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
            {
                user.FailedAttempts++;
                if (user.FailedAttempts >= MaxFailedAttempts)
                    user.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
                await _db.SaveChangesAsync();
                return (LoginResult.InvalidPassword, null);
            }

            user.FailedAttempts = 0;
            user.LockedUntil    = null;
            user.LastLoginAt    = DateTime.UtcNow;

            // Alte abgelaufene Refresh-Tokens aufräumen
            var old = _db.RefreshTokens.Where(r => r.UserId == user.Id && r.ExpiresAt < DateTime.UtcNow);
            _db.RefreshTokens.RemoveRange(old);

            var (access, refresh, expiresAt) = await IssueTokensAsync(user, clientIp);
            await _db.SaveChangesAsync();

            return (LoginResult.Success, new AuthResponse(access, refresh, expiresAt, user.Username));
        }

        // ── Token erneuern ────────────────────────────────────────────────────
        public async Task<AuthResponse?> RefreshAsync(string refreshToken, string clientIp)
        {
            var token = await _db.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (token is null || token.Revoked || token.ExpiresAt < DateTime.UtcNow)
                return null;

            token.Revoked = true; // Rotation: altes Token invalidieren

            var (access, newRefresh, expiresAt) = await IssueTokensAsync(token.User, clientIp);
            await _db.SaveChangesAsync();

            return new AuthResponse(access, newRefresh, expiresAt, token.User.Username);
        }

        // ── Passwort ändern ───────────────────────────────────────────────────
        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null) return false;

            var salt = Convert.FromBase64String(user.PasswordSalt);
            if (!CryptographicOperations.FixedTimeEquals(
                    Pbkdf2(oldPassword, salt),
                    Convert.FromBase64String(user.PasswordHash)))
                return false;

            var newSalt       = RandomNumberGenerator.GetBytes(SaltSize);
            user.PasswordSalt = Convert.ToBase64String(newSalt);
            user.PasswordHash = Convert.ToBase64String(Pbkdf2(newPassword, newSalt));

            // Alle Refresh-Tokens invalidieren (überall ausloggen)
            var tokens = _db.RefreshTokens.Where(r => r.UserId == userId);
            _db.RefreshTokens.RemoveRange(tokens);

            await _db.SaveChangesAsync();
            return true;
        }

        // ── Logout ────────────────────────────────────────────────────────────
        public async Task LogoutAsync(string refreshToken)
        {
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
            if (token is null) return;
            token.Revoked = true;
            await _db.SaveChangesAsync();
        }

        // ── UserId aus JWT Claims ─────────────────────────────────────────────
        public static int GetUserId(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        // ── Interne Helfer ────────────────────────────────────────────────────
        private async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)>
            IssueTokensAsync(User user, string clientIp)
        {
            var key      = GetSigningKey();
            var expiresAt = DateTime.UtcNow.Add(AccessTokenLifetime);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name,           user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var jwt = new JwtSecurityToken(
                issuer:   _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims:   claims,
                expires:  expiresAt,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            var accessToken  = new JwtSecurityTokenHandler().WriteToken(jwt);
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId      = user.Id,
                Token       = refreshToken,
                ExpiresAt   = DateTime.UtcNow.Add(RefreshTokenLifetime),
                CreatedByIp = clientIp,
            });

            return (accessToken, refreshToken, expiresAt);
        }

        private SymmetricSecurityKey GetSigningKey()
        {
            var secret = _config["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret ist nicht konfiguriert.");
            if (secret.Length < 32)
                throw new InvalidOperationException("Jwt:Secret muss mindestens 32 Zeichen lang sein.");
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        }

        private static byte[] Pbkdf2(string password, byte[] salt)
            => Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt, Pbkdf2Iterations,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                HashSize);
    }
}
