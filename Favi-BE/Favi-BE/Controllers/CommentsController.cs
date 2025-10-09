﻿using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
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
        private readonly IPostService _posts;
        private readonly IPrivacyGuard _privacy;

        public CommentsController(ICommentService comments, IPostService posts, IPrivacyGuard privacy)
        {
            _comments = comments;
            _posts = posts;
            _privacy = privacy;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CommentResponse>> Create(CreateCommentRequest dto)
        {
            var userId = User.GetUserIdFromMetadata();
            return Ok(await _comments.CreateAsync(dto.PostId, userId, dto.Content, dto.ParentCommentId));
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateCommentRequest dto)
        {
            var userId = User.GetUserIdFromMetadata();
            var result = await _comments.UpdateAsync(id, userId, dto.Content);
            return result is null
                ? NotFound(new { code = "COMMENT_NOT_FOUND_OR_FORBIDDEN", message = "Không tìm thấy bình luận hoặc bạn không có quyền sửa." })
                : Ok(result);
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserIdFromMetadata();
            var ok = await _comments.DeleteAsync(id, userId);
            return ok
                ? Ok(new { message = "Đã xoá bình luận." })
                : NotFound(new { code = "COMMENT_NOT_FOUND_OR_FORBIDDEN", message = "Không tìm thấy bình luận hoặc bạn không có quyền xoá." });
        }

        [HttpGet("post/{postId}")]
        public async Task<ActionResult<PagedResult<CommentResponse>>> GetByPost(Guid postId, int page = 1, int pageSize = 20)
        {
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserIdFromMetadata() : (Guid?)null;
            var post = await _posts.GetEntityAsync(postId);

            if (post == null)
                return NotFound(new { code = "POST_NOT_FOUND", message = "Bài viết không tồn tại." });

            if (!await _privacy.CanViewPostAsync(post, viewerId))
                return StatusCode(403, new { code = "POST_FORBIDDEN", message = "Bạn không có quyền xem bình luận của bài viết này." });

            return Ok(await _comments.GetByPostAsync(postId, page, pageSize));
        }
    }
}