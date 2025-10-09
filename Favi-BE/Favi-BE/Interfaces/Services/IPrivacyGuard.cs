using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;

namespace Favi_BE.Interfaces.Services
{
    public interface IPrivacyGuard
    {
        Task<bool> CanViewProfileAsync(Profile profile, Guid? viewerId, bool isAdmin = false);
        Task<bool> CanViewPostAsync(Post post, Guid? viewerId, bool isAdmin = false);
        Task<bool> CanViewCollectionAsync(Collection collection, Guid? viewerId, bool isAdmin = false);
        Task<bool> CanViewCommentAsync(Post parentPost, Guid? viewerId, bool isAdmin = false);
        Task<bool> CanFollowAsync(Profile targetProfile, Guid? viewerId, bool isAdmin = false);
        Task<bool> CanViewFollowListAsync(Profile profile, Guid? viewerId, bool isAdmin = false);
        Task<bool> CanReportAsync(ReportTarget targetType, Guid targetId, Guid reporterId);
    }
}
