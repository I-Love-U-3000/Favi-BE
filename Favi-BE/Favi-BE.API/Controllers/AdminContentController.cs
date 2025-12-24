using Favi_BE.Authorization;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

[ApiController]
[Route("api/admin/content")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminContentController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;

    public AdminContentController(IPostService postService, ICommentService commentService)
    {
        _postService = postService;
        _commentService = commentService;
    }

    /// <summary>
    /// Admin: Xóa bài viết (soft delete)
    /// </summary>
    [HttpDelete("posts/{id:guid}")]
    public async Task<IActionResult> DeletePost(Guid id, AdminDeleteContentRequest request)
    {
        var adminId = User.GetUserIdFromMetadata();
        var ok = await _postService.AdminDeleteAsync(id, adminId, request.Reason);
        return ok
            ? Ok(new { message = "Bài viết đã được xóa bởi Admin." })
            : NotFound(new { code = "POST_NOT_FOUND", message = "Không tìm thấy bài viết." });
    }

    /// <summary>
    /// Admin: Xóa bình luận (hard delete)
    /// </summary>
    [HttpDelete("comments/{id:guid}")]
    public async Task<IActionResult> DeleteComment(Guid id, AdminDeleteContentRequest request)
    {
        var adminId = User.GetUserIdFromMetadata();
        var ok = await _commentService.AdminDeleteAsync(id, adminId, request.Reason);
        return ok
            ? Ok(new { message = "Bình luận đã được xóa bởi Admin." })
            : NotFound(new { code = "COMMENT_NOT_FOUND", message = "Không tìm thấy bình luận." });
    }
}
