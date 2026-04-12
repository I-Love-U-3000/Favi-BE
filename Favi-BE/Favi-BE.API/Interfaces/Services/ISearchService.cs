using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Interfaces.Services
{
    public interface ISearchService
    {
        Task<SearchResult> SearchAsync(SearchRequest dto, Guid? userId = null);
        Task<SearchResult> SemanticSearchAsync(SemanticSearchRequest dto, Guid userId);
    }
}
