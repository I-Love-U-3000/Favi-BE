using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _search;
        public SearchController(ISearchService search) => _search = search;

        [HttpPost]
        public async Task<ActionResult<SearchResult>> Search(SearchRequest dto) =>
            Ok(await _search.SearchAsync(dto));

        [Authorize]
        [HttpPost("semantic")]
        public async Task<ActionResult<SearchResult>> SemanticSearch(SemanticSearchRequest dto)
        {
            var userId = User.GetUserIdFromMetadata();
            var result = await _search.SemanticSearchAsync(dto, userId);
            return Ok(result);
        }
    }
}