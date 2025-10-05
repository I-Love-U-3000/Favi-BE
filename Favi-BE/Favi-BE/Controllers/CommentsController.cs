using Favi_BE.Interfaces;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _comments;
        public CommentsController(ICommentService comments) => _comments = comments;

        //[Authorize]
        [HttpPost]
        public async Task<ActionResult<CommentResponse>> Create(CreateCommentRequest dto)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);
            return Ok(await _comments.CreateAsync(dto.PostId, userId, dto.Content, dto.ParentCommentId));
        }

        //[Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateCommentRequest dto)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);
            var result = await _comments.UpdateAsync(id, userId, dto.Content);
            return result is null ? NotFound() : Ok(result);
        }

        //[Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);
            var ok = await _comments.DeleteAsync(id, userId);
            return ok ? Ok() : NotFound();
        }

        [HttpGet("post/{postId}")]
        public async Task<ActionResult<PagedResult<CommentResponse>>> GetByPost(Guid postId, int page = 1, int pageSize = 20) =>
            Ok(await _comments.GetByPostAsync(postId, page, pageSize));
    }
}