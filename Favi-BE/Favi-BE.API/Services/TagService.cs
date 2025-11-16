using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using System.Linq;

namespace Favi_BE.Services
{
    public class TagService : ITagService
    {
        private readonly IUnitOfWork _uow;

        public TagService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<TagResponse> CreateAsync(string name)
        {
            var normalized = name.Trim().ToLowerInvariant();
            var tag = await _uow.Tags.GetByNameAsync(normalized);
            if (tag is null)
            {
                tag = new Tag { Id = Guid.NewGuid(), Name = normalized};
                await _uow.Tags.AddAsync(tag);
                await _uow.CompleteAsync();
            }

            return new TagResponse(tag.Id, tag.Name, 0);
        }

        public async Task<IEnumerable<TagResponse>> GetAllAsync()
        {
            var tags = await _uow.Tags.GetAllAsync();
            return tags.Select(t => new TagResponse(t.Id, t.Name, 0));
        }

        public async Task<PagedResult<TagResponse>> GetAllPagedAsync(int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var (tags, totalCount) = await _uow.Tags.GetAllPagedAsync(skip, pageSize);
            var dtos = tags.Select(t => new TagResponse(t.Id, t.Name, 0));
            return new PagedResult<TagResponse>(dtos, page, pageSize, totalCount);
        }

        public async Task<TagResponse?> GetByIdAsync(Guid id)
        {
            var tag = await _uow.Tags.GetByIdAsync(id);
            if (tag is null) return null;
            return new TagResponse(tag.Id, tag.Name, 0);
        }

        public Task<IEnumerable<TagResponse>> GetOrCreateTagsAsync(IEnumerable<string> names)
        {
            throw new NotImplementedException();
        }

        public async Task<PagedResult<PostResponse>> GetPostsByTagAsync(Guid tagId, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;

            // Lấy danh sách bài viết và tổng số bài
            var (posts, total) = await _uow.Posts.GetPostsByTagPagedAsync(tagId, skip, pageSize);

            var dtos = posts.Select(p =>
            {
                // ✅ Map media
                var medias = p.PostMedias?.Select(m =>
                    new PostMediaResponse(
                        m.Id,
                        m.PostId ?? Guid.Empty,
                        m.Url,
                        m.PublicId,
                        m.Width,
                        m.Height,
                        m.Format,
                        m.Position,
                        m.ThumbnailUrl
                    )
                ).ToList() ?? new List<PostMediaResponse>();

                // ✅ Map tags
                var tags = p.PostTags?.Select(pt =>
                    new TagDto(
                        pt.Tag.Id,
                        pt.Tag.Name
                    )
                ).ToList() ?? new List<TagDto>();

                // ✅ Map reactions
                int totalReactions = p.Reactions?.Count ?? 0;
                var reactionCounts = p.Reactions?
                    .GroupBy(r => r.Type)
                    .ToDictionary(g => g.Key, g => g.Count())
                    ?? new Dictionary<Favi_BE.Models.Enums.ReactionType, int>();

                // Nếu bạn chưa có context người dùng hiện tại, để null
                Favi_BE.Models.Enums.ReactionType? userReaction = null;

                var reactionSummary = new ReactionSummaryDto(
                    totalReactions,
                    reactionCounts,
                    userReaction
                );

                // ✅ Map sang PostResponse đầy đủ
                return new PostResponse(
                    p.Id,
                    p.ProfileId,
                    p.Caption,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.Privacy,
                    medias,
                    tags,
                    reactionSummary,
                    p.Comments.Count
                );
            });

            return new PagedResult<PostResponse>(dtos, page, pageSize, total);
        }


    }
}
