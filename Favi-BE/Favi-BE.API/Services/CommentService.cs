using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using System.Linq;

namespace Favi_BE.Services
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService? _notificationService;
        private readonly IAuditService? _auditService;

        public CommentService(IUnitOfWork uow, INotificationService? notificationService = null, IAuditService? auditService = null)
        {
            _uow = uow;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        public async Task<CommentResponse> CreateAsync(Guid postId, Guid authorId, string content, Guid? parentId, string? mediaUrl = null)
        {
            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                ProfileId = authorId,
                Content = content,
                MediaUrl = mediaUrl,
                ParentCommentId = parentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _uow.Comments.AddAsync(comment);
            await _uow.CompleteAsync();

            // Send notification for new comment (only if not replying to self)
            await _notificationService.CreateCommentNotificationAsync(authorId, postId, comment.Id);

            var summary = await BuildReactionSummaryAsync(comment.Id, authorId);

            return new CommentResponse(comment.Id,
                comment.PostId,
                comment.ProfileId,
                comment.Content,
                comment.MediaUrl,
                comment.CreatedAt,
                comment.UpdatedAt,
                comment.ParentCommentId,
                Reactions: summary);
        }

        public async Task<CommentResponse?> UpdateAsync(Guid commentId, Guid requesterId, string content)
        {
            var comment = await _uow.Comments.GetByIdAsync(commentId);
            if (comment is null || comment.ProfileId != requesterId) return null;

            comment.Content = content;
            comment.UpdatedAt = DateTime.UtcNow;

            _uow.Comments.Update(comment);
            await _uow.CompleteAsync();

            var summary = await BuildReactionSummaryAsync(comment.Id, requesterId);

            return new CommentResponse(comment.Id,
                comment.PostId,
                comment.ProfileId,
                comment.Content,
                comment.MediaUrl,
                comment.CreatedAt,
                comment.UpdatedAt,
                comment.ParentCommentId,
                Reactions: summary);
        }

        public async Task<bool> DeleteAsync(Guid commentId, Guid requesterId)
        {
            var comment = await _uow.Comments.GetByIdAsync(commentId);
            if (comment is null || comment.ProfileId != requesterId) return false;

            _uow.Comments.Remove(comment);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<PagedResult<CommentResponse>> GetByPostAsync(
            Guid currentUserId,
            Guid postId,
            int page,
            int pageSize)
        {
            var (comments, total) = await _uow.Comments.GetCommentsByPostIdAsync(postId, page, pageSize);

            var dtos = new List<CommentResponse>(comments.Count);

            foreach (var c in comments)
            {
                var summary = await BuildReactionSummaryAsync(c.Id, currentUserId);

                var dto = new CommentResponse(
                    c.Id,
                    c.PostId,
                    c.ProfileId,
                    c.Content,
                    c.MediaUrl,
                    c.CreatedAt,
                    c.UpdatedAt,
                    c.ParentCommentId,
                    Reactions: summary
                );

                dtos.Add(dto);
            }

            return new PagedResult<CommentResponse>(dtos, page, pageSize, total);
        }

        public async Task<ReactionSummaryDto> GetReactionsAsync(Guid commentId, Guid? currentUserId)
        {
            var reactions = await _uow.Reactions.GetReactionsByCommentIdAsync(commentId);

            // Đếm tổng số reaction
            var total = reactions.Count();

            // Đếm theo từng ReactionType
            var byType = reactions
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            // Reaction hiện tại của user (nếu có)
            ReactionType? currentUserReaction = null;
            if (currentUserId.HasValue)
            {
                currentUserReaction = reactions
                    .Where(r => r.ProfileId == currentUserId.Value)
                    .Select(r => (ReactionType?)r.Type)
                    .FirstOrDefault();
            }

            return new ReactionSummaryDto(
                total,
                byType,
                currentUserReaction
            );
        }

        public async Task<CommentResponse?> GetByIdAsync(Guid commentId, Guid? currentUserId)
        {
            var comment = await _uow.Comments.GetByIdAsync(commentId);
            if (comment is null) return null;
            var summary = await BuildReactionSummaryAsync(comment.Id, currentUserId);
            return new CommentResponse(comment.Id,
                comment.PostId,
                comment.ProfileId,
                comment.Content,
                comment.MediaUrl,
                comment.CreatedAt,
                comment.UpdatedAt,
                comment.ParentCommentId,
                Reactions: summary);
        }

        public async Task<ReactionType?> ToggleReactionAsync(Guid commentId, Guid userId, ReactionType type)
        {
            var comment = await _uow.Comments.GetByIdAsync(commentId);
            if (comment is null) return null;

            var existing = await _uow.Reactions.GetProfileReactionOnCommentAysnc(userId, commentId);

            // Chưa có → thêm
            if (existing is null)
            {
                await _uow.Reactions.AddAsync(new Reaction
                {
                    CommentId = commentId,
                    ProfileId = userId,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                });
                await _uow.CompleteAsync();

                // Send notification for new comment reaction
                if (_notificationService != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationService.CreateCommentReactionNotificationAsync(userId, commentId);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[CommentService] Error sending notification: {ex.Message}");
                        }
                    });
                }

                return type;
            }

            // Cùng loại → gỡ
            if (existing.Type == type)
            {
                _uow.Reactions.Remove(existing);
                try
                {
                    await _uow.CompleteAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ToggleCommentReaction] Error when removing reaction: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"[ToggleCommentReaction] Inner: {ex.InnerException.Message}");
                    throw;
                }
                return null;
            }

            // Khác loại → đổi
            existing.Type = type;
            _uow.Reactions.Update(existing);
            await _uow.CompleteAsync();
            return type;
        }

        private async Task<ReactionSummaryDto> BuildReactionSummaryAsync(Guid commentId, Guid? currentUserId)
        {
            var reactions = await _uow.Reactions.GetReactionsByCommentIdAsync(commentId);

            var byType = reactions
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            var total = byType.Values.Sum();

            ReactionType? mine = null;
            if (currentUserId.HasValue)
            {
                var my = reactions.FirstOrDefault(r => r.ProfileId == currentUserId.Value);
                if (my != null) mine = my.Type;
            }

            return new ReactionSummaryDto(total, byType, mine);
        }

        // --------------------------------------------------------------------
        // Admin Operations
        // --------------------------------------------------------------------
        public async Task<bool> AdminDeleteAsync(Guid commentId, Guid adminId, string reason)
        {
            var comment = await _uow.Comments.GetByIdAsync(commentId);
            if (comment is null) return false;

            var targetProfileId = comment.ProfileId;

            _uow.Comments.Remove(comment);

            // Log admin action
            if (_auditService != null)
            {
                await _auditService.LogAsync(new Favi_BE.Models.Entities.AdminAction
                {
                    Id = Guid.NewGuid(),
                    AdminId = adminId,
                    ActionType = Models.Enums.AdminActionType.DeleteContent,
                    TargetEntityId = commentId,
                    TargetEntityType = "Comment",
                    TargetProfileId = targetProfileId,
                    Notes = reason,
                    CreatedAt = DateTime.UtcNow
                }, saveChanges: false);
            }

            await _uow.CompleteAsync();
            return true;
        }
        
        public async Task<PagedResult<AnalyticsCommentDto>> GetAllAsync(
            string? search, 
            Guid? postId, 
            Guid? authorId, 
            string? status, 
            DateTime? startDate, 
            DateTime? endDate, 
            int page, 
            int pageSize)
        {
            var query = await _uow.Comments.GetAllAsync();
            var queryable = query.AsQueryable();

            // Filter by search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                queryable = queryable.Where(c => c.Content != null && c.Content.ToLower().Contains(searchLower));
            }

            // Filter by postId
            if (postId.HasValue)
            {
                queryable = queryable.Where(c => c.PostId == postId.Value);
            }

            // Filter by authorId
            if (authorId.HasValue)
            {
                queryable = queryable.Where(c => c.ProfileId == authorId.Value);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                queryable = queryable.Where(c => c.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                queryable = queryable.Where(c => c.CreatedAt <= endDate.Value);
            }

            var total = queryable.Count();
            var skip = (page - 1) * pageSize;
            var items = queryable
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            var allProfiles = await _uow.Profiles.GetAllAsync();
            var profileDict = allProfiles.ToDictionary(p => p.Id, p => p);
            
            var allPosts = await _uow.Posts.GetAllAsync();
            var postDict = allPosts.ToDictionary(p => p.Id, p => p);

            var allReactions = await _uow.Reactions.GetAllAsync();
            var reactionsPerComment = allReactions
                .Where(r => r.CommentId.HasValue)
                .GroupBy(r => r.CommentId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var allComments = await _uow.Comments.GetAllAsync();
            var repliesPerComment = allComments
                .Where(c => c.ParentCommentId.HasValue)
                .GroupBy(c => c.ParentCommentId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var dtos = items.Select(c => {
                profileDict.TryGetValue(c.ProfileId, out var author);
                postDict.TryGetValue(c.PostId, out var post);
                Profile? postAuthor = null;
                if (post != null) profileDict.TryGetValue(post.ProfileId, out postAuthor);

                return new AnalyticsCommentDto(
                    c.Id,
                    c.Content,
                    c.PostId,
                    post != null ? new AnalyticsCommentPostDto(
                        post.Id,
                        post.Caption,
                        postAuthor != null ? new AnalyticsCommentAuthorDto(
                            postAuthor.Id,
                            postAuthor.Username,
                            postAuthor.DisplayName,
                            postAuthor.AvatarUrl
                        ) : null
                    ) : null,
                    new AnalyticsCommentAuthorDto(
                        author?.Id ?? Guid.Empty,
                        author?.Username ?? "N/A",
                        author?.DisplayName,
                        author?.AvatarUrl
                    ),
                    c.ParentCommentId,
                    reactionsPerComment.GetValueOrDefault(c.Id, 0),
                    repliesPerComment.GetValueOrDefault(c.Id, 0),
                    "active", // Default status
                    c.CreatedAt
                );
            }).ToList();

            return new PagedResult<AnalyticsCommentDto>(dtos, page, pageSize, total);
        }
    }
}
