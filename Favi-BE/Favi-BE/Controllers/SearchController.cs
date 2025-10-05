using Favi_BE.Interfaces;
using Favi_BE.Models.Dtos;
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
    }
}