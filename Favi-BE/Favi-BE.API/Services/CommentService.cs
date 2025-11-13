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
            // 1) roots (paged)
            var (roots, totalRoots) = await _uow.Comments.GetRootCommentsPagedAsync(postId, page, pageSize);
            var rootIds = roots.Select(r => r.Id).ToArray();

            // 2) replies của các root trong trang (1 query)
            var replies = await _uow.Comments.GetDirectRepliesForParentsAsync(rootIds);

            // 3) map
            var repliesByParent = replies.GroupBy(r => r.ParentCommentId!.Value)
                                         .ToDictionary(g => g.Key, g => g.Select(c =>
                                             new CommentResponse(c.Id, c.PostId, c.ProfileId, c.Content, c.CreatedAt, c.UpdatedAt, c.ParentCommentId)
                                         ).ToList());

            var dtos = roots.Select(c =>
            {
                var dto = new CommentResponse(c.Id, c.PostId, c.ProfileId, c.Content, c.CreatedAt, c.UpdatedAt, c.ParentCommentId);
                // Nếu bạn đã thêm List<CommentResponse> Replies { get; init; } = new();
                if (repliesByParent.TryGetValue(c.Id, out var children))
                    dto.Replies.AddRange(children);
                return dto;
            }).ToList();

            return new PagedResult<CommentResponse>(dtos, page, pageSize, totalRoots);
        }
    }
}
