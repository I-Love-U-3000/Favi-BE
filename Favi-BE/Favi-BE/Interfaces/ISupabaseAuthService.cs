using Favi_BE.Models.Dtos;

namespace Favi_BE.Interfaces
{
    public interface ISupabaseAuthService
    {
        Task<SupabaseAuthResponse?> RegisterAsync(string email, string password, string username);
        Task<SupabaseAuthResponse?> LoginAsync(string email, string password);
        Task<SupabaseAuthResponse?> RefreshAsync(string refreshToken);
    }
}
