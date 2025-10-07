using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using System.Linq;

namespace Favi_BE.Services
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _uow;

        public CommentService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<CommentResponse> CreateAsync(Guid postId, Guid authorId, string content, Guid? parentId)
        {
            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                ProfileId = authorId,
                Content = content,
                ParentCommentId = parentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _uow.Comments.AddAsync(comment);
            await _uow.CompleteAsync();

            return new CommentResponse(comment.Id, comment.PostId, comment.ProfileId, comment.Content, comment.CreatedAt, comment.UpdatedAt, comment.ParentCommentId);
        }

        public async Task<CommentResponse?> UpdateAsync(Guid commentId, Guid requesterId, string content)
        {
            var comment = await _uow.Comments.GetByIdAsync(commentId);
            if (comment is null || comment.ProfileId != requesterId) return null;

            comment.Content = content;
            comment.UpdatedAt = DateTime.UtcNow;

            _uow.Comments.Update(comment);
            await _uow.CompleteAsync();

            return new CommentResponse(comment.Id, comment.PostId, comment.ProfileId, comment.Content, comment.CreatedAt, comment.UpdatedAt, comment.ParentCommentId);
        }

        public async Task<bool> DeleteAsync(Guid commentId, Guid requesterId)
        {
            var comment = await _uow.Comments.GetByIdAsync(commentId);
            if (comment is null || comment.ProfileId != requesterId) return false;

            _uow.Comments.Remove(comment);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<PagedResult<CommentResponse>> GetByPostAsync(Guid postId, int page, int pageSize)
        {
            var comments = await _uow.Comments.GetCommentsByPostIdAsync(postId);
            var paged = comments.Skip((page - 1) * pageSize).Take(pageSize);

            var dtos = paged.Select(c => new CommentResponse(c.Id, c.PostId, c.ProfileId, c.Content, c.CreatedAt, c.UpdatedAt, c.ParentCommentId));

            return new PagedResult<CommentResponse>(dtos, page, pageSize, comments.Count());
        }
    }
}
