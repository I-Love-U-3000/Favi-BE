using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<ActionResult<PagedResult<TagResponse>> > GetAllPaged(int page = 1, int pageSize = 20) =>
            Ok(await _tags.GetAllPagedAsync(page, pageSize));

        // Không thể load hết 1 lần vì có thể có rất nhiều tag
        /*[HttpGet]
        public async Task<ActionResult<IEnumerable<TagResponse>>> GetAll() =>
            Ok(await _tags.GetAllAsync());*/

        [HttpGet("{id}")]
        public async Task<ActionResult<TagResponse>> GetById(Guid id) =>
            Ok(await _tags.GetByIdAsync(id));

        // Không cần thiết vì tag sẽ được tạo tự động khi tạo bài viết, ngoài ra các quan hệ như post- tag đang chưa được tạo
        /*[Authorize]
        [HttpPost]
        public async Task<ActionResult<TagResponse>> GetOrCreateTagsAsync([FromBody] IEnumerable<Tag> tags) =>
            Ok(await _tags.CreateAsync());*/

        [HttpGet("{id}/posts")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetPosts(Guid id, int page = 1, int pageSize = 20) =>
            Ok(await _tags.GetPostsByTagAsync(id, page, pageSize));
    }
}