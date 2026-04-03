using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VERA.Server.Services;
using VERA.Shared;
using VERA.Shared.Dto;

namespace VERA.Server.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;

        public AuthController(AuthService auth) => _auth = auth;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            var (ok, error) = await _auth.RegisterAsync(req.Username, req.Password);
            if (!ok) return BadRequest(new ApiError("REGISTER_FAILED", error));
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var (result, response) = await _auth.LoginAsync(req.Username, req.Password, ip);
            return result switch
            {
                LoginResult.Success        => Ok(response),
                LoginResult.AccountLocked  => StatusCode(429, new ApiError("LOCKED",    "Account gesperrt. Bitte warte 5 Minuten.")),
                LoginResult.InvalidPassword => Unauthorized(new ApiError("INVALID_PW", "Falsches Passwort.")),
                _                          => NotFound(new ApiError("NOT_FOUND",        "Benutzer nicht gefunden.")),
            };
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var response = await _auth.RefreshAsync(req.RefreshToken, ip);
            if (response is null) return Unauthorized(new ApiError("INVALID_TOKEN", "Token ungültig oder abgelaufen."));
            return Ok(response);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
        {
            await _auth.LogoutAsync(req.RefreshToken);
            return NoContent();
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var userId = AuthService.GetUserId(User);
            var ok = await _auth.ChangePasswordAsync(userId, req.OldPassword, req.NewPassword);
            if (!ok) return BadRequest(new ApiError("WRONG_PW", "Altes Passwort falsch oder Account gesperrt."));
            return NoContent();
        }
    }
}
