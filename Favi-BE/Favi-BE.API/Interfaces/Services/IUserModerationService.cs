using Favi_BE.Models.Dtos;

namespace Favi_BE.Interfaces.Services
{
    public interface IUserModerationService
    {
        Task<UserModerationResponse?> BanAsync(Guid profileId, Guid adminId, BanUserRequest request);
        Task<UserModerationResponse?> WarnAsync(Guid profileId, Guid adminId, WarnUserRequest request);
        Task<bool> UnbanAsync(Guid profileId, Guid adminId, string? reason);
    }
}
