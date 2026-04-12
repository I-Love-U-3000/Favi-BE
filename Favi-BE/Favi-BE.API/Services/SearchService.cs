using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using System.Linq;

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

        public async Task<SearchResult> SearchAsync(SearchRequest dto, Guid? userId = null)
        {
            var query = dto.Query.Trim().ToLowerInvariant();

            switch (dto.Mode)
            {
                case SearchMode.Keyword:
                    return await KeywordSearchAsync(dto, query, userId);
                case SearchMode.Tag:
                    return await TagSearchAsync(dto, query, userId);
                case SearchMode.Semantic:
                    // Semantic search should use the semantic endpoint
                    throw new InvalidOperationException("Use SemanticSearchAsync for semantic search");
                default:
                    throw new ArgumentException("Invalid search mode");
            }
        }

        private async Task<SearchResult> KeywordSearchAsync(SearchRequest dto, string query, Guid? userId = null)
        {
            // Fetch more results to account for privacy filtering
            // Use a multiplier to ensure we get enough viewable posts after filtering
            var fetchMultiplier = 3;
            var fetchSize = dto.PageSize * fetchMultiplier;
            var skip = 0;
            var allFilteredPosts = new List<Post>();
            var maxIterations = 10; // Prevent infinite loops
            var iterations = 0;

            // Keep fetching until we have enough filtered posts or no more results
            while (allFilteredPosts.Count < (dto.Page * dto.PageSize) && iterations < maxIterations)
            {
                var posts = await _uow.Posts.SearchPostsByCaptionAsync(query, skip, fetchSize);

                if (!posts.Any())
                    break;

                // Apply privacy filtering
                foreach (var post in posts)
                {
                    if (await _privacy.CanViewPostAsync(post, userId))
                    {
                        allFilteredPosts.Add(post);
                    }
                }

                skip += fetchSize;
                iterations++;
            }

            // Paginate the filtered results
            var paginatedPosts = allFilteredPosts
                .Skip((dto.Page - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .Select(p => new SearchPostDto(p.Id, p.Caption ?? string.Empty, p.PostMedias?.FirstOrDefault()?.Url ?? string.Empty))
                .ToList();

            // Search tags (name contains)
            var tags = await _uow.Tags.GetAllAsync();
            var matchedTags = tags
                .Where(t => t.Name.ToLower().Contains(query))
                .Select(t => new SearchTagDto(t.Id, t.Name, 0))
                .ToList();

            return new SearchResult(paginatedPosts, matchedTags);
        }

        private async Task<SearchResult> TagSearchAsync(SearchRequest dto, string query, Guid? userId = null)
        {
            // Search for tags that match the query
            var tags = await _uow.Tags.GetAllAsync();
            var matchedTags = tags
                .Where(t => t.Name.ToLower().Contains(query))
                .ToList();

            if (!matchedTags.Any())
            {
                return new SearchResult(Enumerable.Empty<SearchPostDto>(), Enumerable.Empty<SearchTagDto>());
            }

            // Get all tag IDs that match
            var tagIds = matchedTags.Select(t => t.Id).ToList();

            // Fetch more results to account for privacy filtering
            var fetchMultiplier = 3;
            var fetchSize = dto.PageSize * fetchMultiplier;
            var allFilteredPosts = new List<Post>();
            var maxIterations = 10;
            var iterations = 0;
            var page = 1;

            // Keep fetching until we have enough filtered posts or no more results
            while (allFilteredPosts.Count < (dto.Page * dto.PageSize) && iterations < maxIterations)
            {
                var posts = await _uow.Posts.GetPostsByTagIdsPagedAsync(tagIds, page, fetchSize);

                if (!posts.Any())
                    break;

                // Apply privacy filtering
                foreach (var post in posts)
                {
                    if (await _privacy.CanViewPostAsync(post, userId))
                    {
                        allFilteredPosts.Add(post);
                    }
                }

                page++;
                iterations++;
            }

            // Paginate the filtered results
            var paginatedPosts = allFilteredPosts
                .Skip((dto.Page - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .Select(p => new SearchPostDto(
                    p.Id,
                    p.Caption ?? string.Empty,
                    p.PostMedias?.FirstOrDefault()?.Url ?? string.Empty
                ))
                .ToList();

            // Return the tags with post counts
            var tagsWithCounts = matchedTags.Select(t =>
                new SearchTagDto(t.Id, t.Name, t.PostTags?.Count ?? 0)
            );

            return new SearchResult(paginatedPosts, tagsWithCounts);
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
