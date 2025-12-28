using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;

namespace Favi_BE.Services
{
    public class SearchService : ISearchService
    {
        private readonly IUnitOfWork _uow;
        private readonly IVectorIndexService _vectorIndex;
        private readonly IPrivacyGuard _privacy;

        public SearchService(IUnitOfWork uow, IVectorIndexService vectorIndex, IPrivacyGuard privacy)
        {
            _uow = uow;
            _vectorIndex = vectorIndex;
            _privacy = privacy;
        }

        public async Task<SearchResult> SearchAsync(SearchRequest dto)
        {
            var query = dto.Query.Trim().ToLowerInvariant();

            // Search posts (caption contains)
            var posts = await _uow.Posts.GetAllAsync();
            var matchedPosts = posts
                .Where(p => p.Caption != null && p.Caption.ToLower().Contains(query))
                .Skip((dto.Page - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .Select(p => new SearchPostDto(p.Id, p.Caption ?? string.Empty, p.PostMedias?.FirstOrDefault()?.Url ?? string.Empty));

            // Search tags (name contains)
            var tags = await _uow.Tags.GetAllAsync();
            var matchedTags = tags
                .Where(t => t.Name.Contains(query))
                .Select(t => new SearchTagDto(t.Id, t.Name, 0));

            return new SearchResult(matchedPosts, matchedTags);
        }

        public async Task<SearchResult> SemanticSearchAsync(SemanticSearchRequest dto, Guid userId)
        {
            var query = dto.Query.Trim();
            var page = dto.Page ?? 1;
            var pageSize = dto.PageSize ?? 20;
            var k = dto.K ?? 100;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (k < 1) k = 100;

            // Get semantic search results from vector API
            var vectorResults = await _vectorIndex.SearchAsync(userId, query, k);

            if (!vectorResults.Any())
            {
                // No results from vector search - return empty
                return new SearchResult(
                    Enumerable.Empty<SearchPostDto>(),
                    Enumerable.Empty<SearchTagDto>()
                );
            }

            // Extract post IDs and convert to Guids
            var postIds = vectorResults
                .Select(r => Guid.TryParse(r.PostId, out var guid) ? (Guid?)guid : null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();

            if (!postIds.Any())
            {
                return new SearchResult(
                    Enumerable.Empty<SearchPostDto>(),
                    Enumerable.Empty<SearchTagDto>()
                );
            }

            // Fetch posts from database
            var posts = await _uow.Posts.GetAllAsync();
            var matchedPosts = new List<SearchPostDto>();

            // Create score lookup for ordering (handle duplicates by taking highest score)
            var scoreMap = vectorResults
                .GroupBy(r => Guid.TryParse(r.PostId, out var g) ? g : Guid.Empty)
                .Where(g => g.Key != Guid.Empty)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(r => r.Score)
                );

            foreach (var postId in postIds)
            {
                var post = posts.FirstOrDefault(p => p.Id == postId);
                if (post == null) continue;

                // Check privacy permissions
                if (!await _privacy.CanViewPostAsync(post, userId))
                    continue;

                matchedPosts.Add(new SearchPostDto(
                    post.Id,
                    post.Caption ?? string.Empty,
                    post.PostMedias?.FirstOrDefault()?.Url ?? string.Empty
                ));
            }

            // Maintain vector search order (by score, descending)
            var orderedPosts = matchedPosts
                .OrderByDescending(p => scoreMap.GetValueOrDefault(p.Id, 0))
                .ToList();

            // Apply pagination
            var skip = (page - 1) * pageSize;
            var pagedPosts = orderedPosts
                .Skip(skip)
                .Take(pageSize);

            // Semantic search doesn't return tags
            return new SearchResult(
                pagedPosts,
                Enumerable.Empty<SearchTagDto>()
            );
        }
    }
}
