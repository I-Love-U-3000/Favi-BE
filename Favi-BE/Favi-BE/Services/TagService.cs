using Favi_BE.Interfaces;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;

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

        public async Task<TagResponse?> GetByIdAsync(Guid id)
        {
            var tag = await _uow.Tags.GetByIdAsync(id);
            if (tag is null) return null;
            return new TagResponse(tag.Id, tag.Name, 0);
        }

        public async Task<PagedResult<PostResponse>> GetPostsByTagAsync(Guid tagId, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var posts = await _uow.Posts.GetPostsByTagIdAsync(tagId, skip, pageSize);

            var dtos = posts.Select(p => new PostResponse(
                p.Id,
                p.ProfileId,
                p.Caption,
                p.CreatedAt,
                p.UpdatedAt,
                p.Privacy,
                new List<PostMediaResponse>(),
                new List<TagDto>(),
                new ReactionSummaryDto(0, new(), null)
            ));

            return new PagedResult<PostResponse>(dtos, page, pageSize, -1);
        }
    }
}
