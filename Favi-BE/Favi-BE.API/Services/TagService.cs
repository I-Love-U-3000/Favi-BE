using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
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

        // ------------------------------------------------------
        // Tags CRUD / Queries
        // ------------------------------------------------------
        public async Task<TagResponse> CreateAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tag name is required", nameof(name));

            var normalized = name.Trim().ToLowerInvariant();
            var tag = await _uow.Tags.GetByNameAsync(normalized);
            if (tag is null)
            {
                tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = normalized
                };
                await _uow.Tags.AddAsync(tag);
                await _uow.CompleteAsync();
            }

            // hiện chưa tính số post, để 0
            return new TagResponse(tag.Id, tag.Name, 0);
        }

        public async Task<IEnumerable<TagResponse>> GetAllAsync()
        {
            var tags = await _uow.Tags.GetAllAsync();
            return tags.Select(t => new TagResponse(t.Id, t.Name, 0));
        }

        public async Task<PagedResult<TagResponse>> GetAllPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

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

        // 🆕: wrapper cho _uow.Tags.GetOrCreateTagsAsync, trả về TagResponse
        public async Task<IEnumerable<TagResponse>> GetOrCreateTagsAsync(IEnumerable<string> names)
        {
            if (names == null) return Enumerable.Empty<TagResponse>();

            var normalizedNames = names
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (!normalizedNames.Any())
                return Enumerable.Empty<TagResponse>();

            var entities = await _uow.Tags.GetOrCreateTagsAsync(normalizedNames);

            // hiện chưa tính số post, để 0
            return entities.Select(t => new TagResponse(t.Id, t.Name, 0));
        }

        // ------------------------------------------------------
        // Posts by Tag
        // ------------------------------------------------------
        public async Task<PagedResult<PostResponse>> GetPostsByTagAsync(Guid tagId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var skip = (page - 1) * pageSize;

            // Lấy danh sách bài viết và tổng số bài
            var (posts, total) = await _uow.Posts.GetPostsByTagPagedAsync(tagId, skip, pageSize);

            var dtos = posts.Select(p =>
            {
                // Media
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

                // Tags
                var tags = p.PostTags?.Select(pt =>
                    new TagDto(
                        pt.Tag.Id,
                        pt.Tag.Name
                    )
                ).ToList() ?? new List<TagDto>();

                // Reactions
                int totalReactions = p.Reactions?.Count ?? 0;
                var reactionCounts = p.Reactions?
                    .GroupBy(r => r.Type)
                    .ToDictionary(g => g.Key, g => g.Count())
                    ?? new Dictionary<ReactionType, int>();

                // chưa có context user hiện tại → null
                ReactionType? userReaction = null;

                var reactionSummary = new ReactionSummaryDto(
                    totalReactions,
                    reactionCounts,
                    userReaction
                );

                // 🆕 Location mapping
                LocationDto? location = null;
                if (p.LocationName != null ||
                    p.LocationFullAddress != null ||
                    p.LocationLatitude != null ||
                    p.LocationLongitude != null)
                {
                    location = new LocationDto(
                        p.LocationName,
                        p.LocationFullAddress,
                        p.LocationLatitude,
                        p.LocationLongitude
                    );
                }

                // PostResponse đầy đủ (có Location)
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
                    p.Comments.Count,
                    location
                );
            });

            return new PagedResult<PostResponse>(dtos, page, pageSize, total);
        }
    }
}