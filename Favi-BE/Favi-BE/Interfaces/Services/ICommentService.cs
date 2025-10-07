using Favi_BE.Models.Dtos;

namespace Favi_BE.Interfaces.Services
{
    public interface ICommentService
    {
        Task<CommentResponse> CreateAsync(Guid postId, Guid authorId, string content, Guid? parentId);
        Task<CommentResponse?> UpdateAsync(Guid commentId, Guid requesterId, string content);
        Task<bool> DeleteAsync(Guid commentId, Guid requesterId);
        Task<PagedResult<CommentResponse>> GetByPostAsync(Guid postId, int page, int pageSize);
    }

}
