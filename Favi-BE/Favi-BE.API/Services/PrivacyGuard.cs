using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;

namespace Favi_BE.Services
{
    public class PrivacyGuard : IPrivacyGuard
    {
        private readonly IUnitOfWork _uow;
        public PrivacyGuard(IUnitOfWork uow) => _uow = uow;

        // ===== PROFILE =====
        public async Task<bool> CanViewProfileAsync(Profile profile, Guid? viewerId, bool isAdmin = false)
        {
            if (profile == null) return false;
            if (isAdmin) return true;

            var privacy = profile.PrivacyLevel; // Nếu chưa có property này, mặc định Public
            if (!viewerId.HasValue) return privacy == PrivacyLevel.Public;
            if (viewerId.Value == profile.Id) return true;

            return privacy switch
            {
                PrivacyLevel.Public => true,
                PrivacyLevel.Followers => await _uow.Follows.IsFollowingAsync(viewerId.Value, profile.Id),
                PrivacyLevel.Private => false,
                _ => false
            };
        }

        // ===== POST =====
        public async Task<bool> CanViewPostAsync(Post post, Guid? viewerId, bool isAdmin = false)
        {
            if (post == null) return false;
            if (isAdmin) return true;

            var ownerId = post.ProfileId;
            if (!viewerId.HasValue) return post.Privacy == PrivacyLevel.Public;
            if (viewerId.Value == ownerId) return true;

            return post.Privacy switch
            {
                PrivacyLevel.Public => true,
                PrivacyLevel.Followers => await _uow.Follows.IsFollowingAsync(viewerId.Value, ownerId),
                PrivacyLevel.Private => false,
                _ => false
            };
        }

        // ===== COLLECTION =====
        public async Task<bool> CanViewCollectionAsync(Collection collection, Guid? viewerId, bool isAdmin = false)
        {
            if (collection == null) return false;
            if (isAdmin) return true;

            var ownerId = collection.ProfileId;
            if (!viewerId.HasValue) return collection.PrivacyLevel == PrivacyLevel.Public;
            if (viewerId.Value == ownerId) return true;

            return collection.PrivacyLevel switch
            {
                PrivacyLevel.Public => true,
                PrivacyLevel.Followers => await _uow.Follows.IsFollowingAsync(viewerId.Value, ownerId),
                PrivacyLevel.Private => false,
                _ => false
            };
        }

        // ===== COMMENT =====
        public async Task<bool> CanViewCommentAsync(Post parentPost, Guid? viewerId, bool isAdmin = false)
        {
            if (parentPost == null) return false;
            // Comment kế thừa quyền từ Post cha
            return await CanViewPostAsync(parentPost, viewerId, isAdmin);
        }

        // ===== FOLLOW =====
        public async Task<bool> CanFollowAsync(Profile targetProfile, Guid? viewerId, bool isAdmin = false)
        {
            if (targetProfile == null) return false;
            if (isAdmin) return true;

            var privacy = targetProfile.PrivacyLevel; // Nếu chưa có field này, tạm xem là Public
            if (!viewerId.HasValue) return privacy == PrivacyLevel.Public;
            if (viewerId.Value == targetProfile.Id) return false; // Không follow chính mình

            return privacy switch
            {
                PrivacyLevel.Public => true,
                PrivacyLevel.Followers => true, // yêu cầu approve sau này
                PrivacyLevel.Private => false,
                _ => false
            };
        }
        public async Task<bool> CanViewFollowListAsync(Profile profile, Guid? viewerId, bool isAdmin = false)
        {
            if (profile == null) return false;
            if (isAdmin) return true;

            var privacy = profile.FollowPrivacyLevel;
            if (!viewerId.HasValue) return privacy == PrivacyLevel.Public;
            if (viewerId.Value == profile.Id) return true;

            return privacy switch
            {
                PrivacyLevel.Public => true,
                PrivacyLevel.Followers => await _uow.Follows.IsFollowingAsync(viewerId.Value, profile.Id),
                PrivacyLevel.Private => false,
                _ => false
            };
        }
        public async Task<bool> CanReportAsync(ReportTarget targetType, Guid targetId, Guid reporterId)
        {
            var targetProfile = await _uow.Profiles.GetByIdAsync(targetId);
            var collection = await _uow.Collections.GetByIdAsync(targetId);
            switch (targetType)
            {
                case ReportTarget.Post:
                    var post = await _uow.Posts.GetByIdAsync(targetId);
                    return post != null && await CanViewPostAsync(post, reporterId);

                case ReportTarget.User:
                    return await CanViewProfileAsync(targetProfile, reporterId);

                case ReportTarget.Comment:
                    var comment = await _uow.Comments.GetByIdAsync(targetId);
                    if (comment == null) return false;
                    var parentPost = await _uow.Posts.GetByIdAsync(comment.PostId);
                    return parentPost != null && await CanViewPostAsync(parentPost, reporterId);

                case ReportTarget.Collection:
                    return await CanViewCollectionAsync(collection, reporterId);

                default:
                    return false;
            }
        }

    }
}
