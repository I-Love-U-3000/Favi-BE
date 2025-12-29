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

        public async Task<bool> CanViewProfileAsync(Profile profile, Guid? viewerId, bool isAdmin = false)
        {
            if (profile == null)
                return false;

            var (viewerIsAdmin, viewerIsBanned) = await ResolveViewerAsync(viewerId, isAdmin);
            if (viewerIsBanned)
                return false;
            if (viewerIsAdmin)
                return true;

            if (IsBanActive(profile))
                return false;

            if (!viewerId.HasValue)
                return profile.PrivacyLevel == PrivacyLevel.Public;

            if (viewerId.Value == profile.Id)
                return true;

            return profile.PrivacyLevel switch
            {
                PrivacyLevel.Public => true,
                PrivacyLevel.Followers => await _uow.Follows.IsFollowingAsync(viewerId.Value, profile.Id),
                PrivacyLevel.Private => false,
                _ => false
            };
        }

        public async Task<bool> CanViewPostAsync(Post post, Guid? viewerId, bool isAdmin = false)
        {
            if (post == null)
                return false;

            var (viewerIsAdmin, viewerIsBanned) = await ResolveViewerAsync(viewerId, isAdmin);
            if (viewerIsBanned)
                return false;
            if (viewerIsAdmin)
                return true;

            if (await IsOwnerBannedAsync(post.Profile, post.ProfileId))
                return false;

            if (!viewerId.HasValue)
                return post.Privacy == PrivacyLevel.Public;

            if (viewerId.Value == post.ProfileId)
                return true;

            return post.Privacy switch
            {
                PrivacyLevel.Public => true,
                PrivacyLevel.Followers => await _uow.Follows.IsFollowingAsync(viewerId.Value, post.ProfileId),
                PrivacyLevel.Private => false,
                _ => false
            };
        }

        public async Task<bool> CanViewCollectionAsync(Collection collection, Guid? viewerId, bool isAdmin = false)
        {
            if (collection == null)
                return false;

            var (viewerIsAdmin, viewerIsBanned) = await ResolveViewerAsync(viewerId, isAdmin);
            if (viewerIsBanned)
                return false;
            if (viewerIsAdmin)
                return true;

            if (await IsOwnerBannedAsync(collection.Profile, collection.ProfileId))
                return false;

            if (!viewerId.HasValue)
                return collection.PrivacyLevel == PrivacyLevel.Public;

            if (viewerId.Value == collection.ProfileId)
                return true;

            return collection.PrivacyLevel switch
            {
                PrivacyLevel.Public => true,
                PrivacyLevel.Followers => await _uow.Follows.IsFollowingAsync(viewerId.Value, collection.ProfileId),
                PrivacyLevel.Private => false,
                _ => false
            };
        }

        public async Task<bool> CanViewStoryAsync(Story story, Guid? viewerId, bool isAdmin = false)
        {
            if (story == null)
                return false;

            var (viewerIsAdmin, viewerIsBanned) = await ResolveViewerAsync(viewerId, isAdmin);
            if (viewerIsBanned)
                return false;
            if (viewerIsAdmin)
                return true;

            if (await IsOwnerBannedAsync(story.Profile, story.ProfileId))
                return false;

            // Archived stories only visible to owner
            if (story.IsArchived)
                return viewerId.HasValue && viewerId.Value == story.ProfileId;

            // Expired stories only visible to owner
            if (story.ExpiresAt <= DateTime.UtcNow)
                return viewerId.HasValue && viewerId.Value == story.ProfileId;

            if (!viewerId.HasValue)
                return story.Privacy == PrivacyLevel.Public;

            if (viewerId.Value == story.ProfileId)
                return true;

            return story.Privacy switch
            {
                PrivacyLevel.Public => true,
                PrivacyLevel.Followers => await _uow.Follows.IsFollowingAsync(viewerId.Value, story.ProfileId),
                PrivacyLevel.Private => false,
                _ => false
            };
        }

        public async Task<bool> CanViewCommentAsync(Post parentPost, Guid? viewerId, bool isAdmin = false)
        {
            if (parentPost == null)
                return false;

            return await CanViewPostAsync(parentPost, viewerId, isAdmin);
        }

        public async Task<bool> CanFollowAsync(Profile targetProfile, Guid? viewerId, bool isAdmin = false)
        {
            if (targetProfile == null)
                return false;

            var (viewerIsAdmin, viewerIsBanned) = await ResolveViewerAsync(viewerId, isAdmin);
            if (viewerIsBanned)
                return false;
            if (viewerIsAdmin)
                return true;

            if (IsBanActive(targetProfile))
                return false;

            var privacy = targetProfile.PrivacyLevel;
            if (!viewerId.HasValue)
                return privacy == PrivacyLevel.Public;

            if (viewerId.Value == targetProfile.Id)
                return false;

            return privacy switch
            {
                PrivacyLevel.Public => true,
                PrivacyLevel.Followers => true,
                PrivacyLevel.Private => false,
                _ => false
            };
        }

        public async Task<bool> CanViewFollowListAsync(Profile profile, Guid? viewerId, bool isAdmin = false)
        {
            if (profile == null)
                return false;

            var (viewerIsAdmin, viewerIsBanned) = await ResolveViewerAsync(viewerId, isAdmin);
            if (viewerIsBanned)
                return false;
            if (viewerIsAdmin)
                return true;

            if (IsBanActive(profile))
                return false;

            var privacy = profile.FollowPrivacyLevel;
            if (!viewerId.HasValue)
                return privacy == PrivacyLevel.Public;

            if (viewerId.Value == profile.Id)
                return true;

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
            var (reporterIsAdmin, reporterIsBanned) = await ResolveViewerAsync(reporterId, false);
            if (reporterIsBanned)
                return false;

            switch (targetType)
            {
                case ReportTarget.Post:
                    var post = await _uow.Posts.GetByIdAsync(targetId);
                    return post != null && await CanViewPostAsync(post, reporterId, reporterIsAdmin);

                case ReportTarget.User:
                    var targetProfile = await _uow.Profiles.GetByIdAsync(targetId);
                    return targetProfile != null && await CanViewProfileAsync(targetProfile, reporterId, reporterIsAdmin);

                case ReportTarget.Comment:
                    var comment = await _uow.Comments.GetByIdAsync(targetId);
                    if (comment == null)
                        return false;
                    var parentPost = await _uow.Posts.GetByIdAsync(comment.PostId);
                    return parentPost != null && await CanViewPostAsync(parentPost, reporterId, reporterIsAdmin);

                case ReportTarget.Collection:
                    var collection = await _uow.Collections.GetByIdAsync(targetId);
                    return collection != null && await CanViewCollectionAsync(collection, reporterId, reporterIsAdmin);

                default:
                    return false;
            }
        }

        private async Task<(bool IsAdmin, bool IsBanned)> ResolveViewerAsync(Guid? viewerId, bool isAdminOverride)
        {
            if (isAdminOverride)
                return (true, false);

            if (!viewerId.HasValue)
                return (false, false);

            var viewer = await _uow.Profiles.GetByIdAsync(viewerId.Value);
            if (viewer == null)
                return (false, false);

            if (viewer.Role == UserRole.Admin)
                return (true, false);

            return (false, IsBanActive(viewer));
        }

        private async Task<bool> IsOwnerBannedAsync(Profile? owner, Guid ownerId)
        {
            if (owner == null)
                owner = await _uow.Profiles.GetByIdAsync(ownerId);

            return IsBanActive(owner);
        }

        private static bool IsBanActive(Profile? profile)
        {
            if (profile == null)
                return false;

            if (!profile.IsBanned)
                return false;

            if (!profile.BannedUntil.HasValue)
                return true;

            return profile.BannedUntil > DateTime.UtcNow;
        }
    }
}
