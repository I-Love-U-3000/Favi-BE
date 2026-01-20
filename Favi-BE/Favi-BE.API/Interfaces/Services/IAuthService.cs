using Favi_BE.Models.Dtos;

namespace Favi_BE.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(string emailOrUsername, string password);
    Task<AuthResponse?> RegisterAsync(string email, string password, string username, string? displayName);
    Task<AuthResponse?> RefreshAsync(string refreshToken);
}
