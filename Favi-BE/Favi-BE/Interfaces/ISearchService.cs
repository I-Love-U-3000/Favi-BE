using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Interfaces
{
    public interface ISearchService
    {
        Task<SearchResult> SearchAsync(SearchRequest dto); 
    }
}
