using Favi_BE.Interfaces;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tags;
        public TagsController(ITagService tags) => _tags = tags;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagResponse>>> GetAll() =>
            Ok(await _tags.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<TagResponse>> GetById(Guid id) =>
            Ok(await _tags.GetByIdAsync(id));

        [HttpPost]
        public async Task<ActionResult<TagResponse>> Create(string name) =>
            Ok(await _tags.CreateAsync(name));

        [HttpGet("{id}/posts")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetPosts(Guid id, int page = 1, int pageSize = 20) =>
            Ok(await _tags.GetPostsByTagAsync(id, page, pageSize));
    }
}