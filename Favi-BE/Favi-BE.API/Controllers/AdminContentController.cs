using Favi_BE.Authorization;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

/// <summary>
/// Admin endpoints for content management (posts, comments)
/// </summary>
[ApiController]
[Route("api/admin/content")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminContentController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;
    private readonly IBulkActionService _bulkService;

    public AdminContentController(
        IPostService postService,
        ICommentService commentService,
        IBulkActionService bulkService)
    {
        _postService = postService;
        _commentService = commentService;
        _bulkService = bulkService;
    }

    // ============================================================
    // SINGLE CONTENT ACTIONS
    // ============================================================

    /// <summary>
    /// Admin: Delete a single post (soft delete)
    /// </summary>
    [HttpDelete("posts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(Guid id, AdminDeleteContentRequest request)
    {
        var adminId = User.GetUserId();
        var ok = await _postService.AdminDeleteAsync(id, adminId, request.Reason);
        return ok
            ? Ok(new { message = "Bài viết đã được xóa bởi Admin." })
            : NotFound(new { code = "POST_NOT_FOUND", message = "Không tìm thấy bài viết." });
    }

    /// <summary>
    /// Admin: Delete a single comment (hard delete)
    /// </summary>
    [HttpDelete("comments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(Guid id, AdminDeleteContentRequest request)
    {
        var adminId = User.GetUserId();
        var ok = await _commentService.AdminDeleteAsync(id, adminId, request.Reason);
        return ok
            ? Ok(new { message = "Bình luận đã được xóa bởi Admin." })
            : NotFound(new { code = "COMMENT_NOT_FOUND", message = "Không tìm thấy bình luận." });
    }

    // ============================================================
    // BULK CONTENT ACTIONS
    // ============================================================

    /// <summary>
    /// Admin: Delete multiple posts at once (soft delete, max 100)
    /// </summary>
    [HttpPost("posts/bulk/delete")]
    [ProducesResponseType(typeof(BulkActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkActionResponse>> BulkDeletePosts([FromBody] BulkDeletePostsRequest request)
    {
        if (request.PostIds == null || !request.PostIds.Any())
            return BadRequest(new { code = "NO_POST_IDS", message = "At least one post ID is required." });

        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { code = "REASON_REQUIRED", message = "Reason is required." });

        var adminId = User.GetUserIdFromMetadata();
        var result = await _bulkService.BulkDeletePostsAsync(
            request.PostIds,
            adminId,
            request.Reason);

        return Ok(result);
    }

    /// <summary>
    /// Admin: Delete multiple comments at once (hard delete, max 100)
    /// </summary>
    [HttpPost("comments/bulk/delete")]
    [ProducesResponseType(typeof(BulkActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkActionResponse>> BulkDeleteComments([FromBody] BulkDeleteCommentsRequest request)
    {
        if (request.CommentIds == null || !request.CommentIds.Any())
            return BadRequest(new { code = "NO_COMMENT_IDS", message = "At least one comment ID is required." });

        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { code = "REASON_REQUIRED", message = "Reason is required." });

        var adminId = User.GetUserIdFromMetadata();
        var result = await _bulkService.BulkDeleteCommentsAsync(
            request.CommentIds,
            adminId,
            request.Reason);

        return Ok(result);
    }
}
