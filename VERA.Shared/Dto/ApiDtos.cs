using System.ComponentModel.DataAnnotations;

namespace VERA.Shared.Dto
{
    public record RegisterRequest(
        [Required, StringLength(50, MinimumLength = 3)] string Username,
        [Required, StringLength(128, MinimumLength = 8)] string Password);

    public record LoginRequest(
        [Required, StringLength(50, MinimumLength = 1)] string Username,
        [Required, StringLength(128, MinimumLength = 1)] string Password);

    public record RefreshRequest(
        [Required] string RefreshToken);

    public record AuthResponse(
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt,
        string Username);

    public record ChangePasswordRequest(
        [Required] string OldPassword,
        [Required, StringLength(128, MinimumLength = 8)] string NewPassword);

    public record TimeEntryDto(
        Guid      Id,
        [Required, StringLength(200)] string Title,
        [Required, StringLength(100)] string Category,
        DateTime  StartTime,
        DateTime? EndTime,
        int       Type);

    public record UpsertTimeEntryRequest(
        Guid?     Id,
        [Required, StringLength(200)] string Title,
        [Required, StringLength(100)] string Category,
        DateTime  StartTime,
        DateTime? EndTime,
        [Range(0, 10)] int Type);

    public record ApiError(string Code, string Message);
}
