using Favi_BE.Interfaces;
using Favi_BE.Models.Dtos;

namespace Favi_BE.Services
{
    public class SearchService : ISearchService
    {
        private readonly IUnitOfWork _uow;

        public SearchService(IUnitOfWork uow)
        {
            _uow = uow;
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
    }
}
