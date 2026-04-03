using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VERA.Shared;

namespace VERA.Services
{
    public class AccountService
    {
        // ── Sicherheitsparameter ──────────────────────────────────────────────
        private const int SaltSize          = 32;          // 256 Bit
        private const int HashSize          = 32;          // 256 Bit
        private const int Pbkdf2Iterations  = 300_000;     // OWASP-Empfehlung 2024
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _accountFile;

        public AccountService()
        {
            _accountFile = Path.Combine(FileSystem.AppDataDirectory, "vera_account.dat");
        }

        // ── Öffentliche API ───────────────────────────────────────────────────

        /// <summary>True wenn bereits ein Account angelegt wurde.</summary>
        public bool AccountExists() => File.Exists(_accountFile);

        /// <summary>Erstellt einen neuen Account. Gibt false zurück wenn bereits einer existiert.</summary>
        public async Task<bool> RegisterAsync(string username, string password)
        {
            if (AccountExists()) return false;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return false;

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Pbkdf2(password, salt);

            var data = new AccountData
            {
                Username      = username.Trim(),
                PasswordSalt  = Convert.ToBase64String(salt),
                PasswordHash  = Convert.ToBase64String(hash),
                CreatedAt     = DateTime.UtcNow,
                FailedAttempts = 0,
                LockedUntil   = null
            };

            await SaveAsync(data);
            return true;
        }

        /// <summary>Prüft das Passwort und gibt das Ergebnis zurück.</summary>
        public async Task<(LoginResult Result, string Username)> LoginAsync(string password)
        {
            if (!AccountExists()) return (LoginResult.NoAccountFound, string.Empty);

            var data = await LoadAsync();
            if (data is null) return (LoginResult.NoAccountFound, string.Empty);

            // Lockout-Check
            if (data.LockedUntil.HasValue && data.LockedUntil.Value > DateTime.UtcNow)
                return (LoginResult.AccountLocked, string.Empty);

            // Alten Lockout aufheben
            if (data.LockedUntil.HasValue && data.LockedUntil.Value <= DateTime.UtcNow)
            {
                data.LockedUntil    = null;
                data.FailedAttempts = 0;
            }

            var salt         = Convert.FromBase64String(data.PasswordSalt);
            var expectedHash = Convert.FromBase64String(data.PasswordHash);
            var actualHash   = Pbkdf2(password, salt);

            if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
            {
                data.FailedAttempts++;
                if (data.FailedAttempts >= MaxFailedAttempts)
                    data.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);

                await SaveAsync(data);
                return (LoginResult.InvalidPassword, string.Empty);
            }

            // Erfolg → Zähler zurücksetzen
            data.FailedAttempts = 0;
            data.LockedUntil    = null;
            data.LastLoginAt    = DateTime.UtcNow;
            await SaveAsync(data);

            return (LoginResult.Success, data.Username);
        }

        /// <summary>Gibt Benutzernamen zurück oder leer wenn kein Account.</summary>
        public async Task<string> GetUsernameAsync()
        {
            if (!AccountExists()) return string.Empty;
            var data = await LoadAsync();
            return data?.Username ?? string.Empty;
        }

        /// <summary>Gibt verbleibende Sperr-Sekunden zurück (0 = nicht gesperrt).</summary>
        public async Task<int> GetLockoutSecondsRemainingAsync()
        {
            if (!AccountExists()) return 0;
            var data = await LoadAsync();
            if (data?.LockedUntil == null) return 0;
            var remaining = data.LockedUntil.Value - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? (int)remaining.TotalSeconds : 0;
        }

        /// <summary>Ändert das Passwort (altes Passwort wird geprüft).</summary>
        public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            var (result, _) = await LoginAsync(oldPassword);
            if (result != LoginResult.Success) return false;

            var data = await LoadAsync();
            if (data is null) return false;

            var salt             = RandomNumberGenerator.GetBytes(SaltSize);
            data.PasswordSalt    = Convert.ToBase64String(salt);
            data.PasswordHash    = Convert.ToBase64String(Pbkdf2(newPassword, salt));
            data.FailedAttempts  = 0;
            data.LockedUntil     = null;

            await SaveAsync(data);
            return true;
        }

        /// <summary>Löscht den Account vollständig (für Reset).</summary>
        public void DeleteAccount()
        {
            if (File.Exists(_accountFile))
                File.Delete(_accountFile);
        }

        // ── Interne Helfer ────────────────────────────────────────────────────

        private static byte[] Pbkdf2(string password, byte[] salt)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            return Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes, salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                HashSize);
        }

        private async Task<AccountData?> LoadAsync()
        {
            var json = await File.ReadAllTextAsync(_accountFile);
            return JsonSerializer.Deserialize<AccountData>(json, _json);
        }

        private async Task SaveAsync(AccountData data)
        {
            var json   = JsonSerializer.Serialize(data, _json);
            var tmp    = _accountFile + ".tmp";
            await File.WriteAllTextAsync(tmp, json);
            File.Move(tmp, _accountFile, overwrite: true);
        }

        // ── Datenmodell ───────────────────────────────────────────────────────
        private class AccountData
        {
            public string    Username       { get; set; } = string.Empty;
            public string    PasswordSalt   { get; set; } = string.Empty;
            public string    PasswordHash   { get; set; } = string.Empty;
            public DateTime  CreatedAt      { get; set; }
            public DateTime? LastLoginAt    { get; set; }
            public int       FailedAttempts { get; set; }
            public DateTime? LockedUntil    { get; set; }
        }
    }
}
