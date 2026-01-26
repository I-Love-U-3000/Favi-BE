using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;

namespace Favi_BE.Interfaces.Services
{
    public interface ICommentService
    {
        Task<CommentResponse?> GetByIdAsync(Guid commentId, Guid? currentUserId);
        Task<CommentResponse> CreateAsync(Guid postId, Guid authorId, string content, Guid? parentId, string? mediaUrl = null);
        Task<CommentResponse?> UpdateAsync(Guid commentId, Guid requesterId, string content);
        Task<bool> DeleteAsync(Guid commentId, Guid requesterId);
        Task<ReactionSummaryDto> GetReactionsAsync(Guid commentId, Guid? currentUserId);
        Task<PagedResult<CommentResponse>> GetByPostAsync(Guid currentUserId, Guid postId, int page, int pageSize);
        Task<ReactionType?> ToggleReactionAsync(Guid commentId, Guid userId, ReactionType type);
        Task<IEnumerable<CommentReactorResponse>> GetReactorsAsync(Guid commentId, Guid requesterId);
        Task<bool> AdminDeleteAsync(Guid commentId, Guid adminId, string reason);
        Task<PagedResult<AnalyticsCommentDto>> GetAllAsync(
            string? search,
            Guid? postId,
            Guid? authorId,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize);
        Task<CommentStatsDto> GetStatsAsync();
    }
}
