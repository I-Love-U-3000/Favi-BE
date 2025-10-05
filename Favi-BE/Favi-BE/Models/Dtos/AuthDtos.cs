namespace Favi_BE.Models.Dtos
{
    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string Email, string Password, string Username, string? DisplayName);
    public record RefreshRequest(string RefreshToken);

    public record AuthResponse(
        string AccessToken,
        string RefreshToken,
        string Message
    );
}
