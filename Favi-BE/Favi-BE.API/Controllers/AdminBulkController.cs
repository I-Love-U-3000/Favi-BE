using Favi_BE.Authorization;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

/// <summary>
/// Admin endpoints for bulk operations
/// </summary>
[ApiController]
[Route("api/admin/bulk")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminBulkController : ControllerBase
{
    private readonly IBulkActionService _bulkService;

    public AdminBulkController(IBulkActionService bulkService)
    {
        _bulkService = bulkService;
    }

    // ============================================================
    // USER MODERATION
    // ============================================================

    /// <summary>
    /// Ban multiple users at once
    /// </summary>
    /// <remarks>
    /// Maximum 100 users per request. Users already banned will be skipped.
    /// </remarks>
    [HttpPost("users/ban")]
    [ProducesResponseType(typeof(BulkActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkActionResponse>> BulkBan([FromBody] BulkBanRequest request)
    {
        if (request.ProfileIds == null || !request.ProfileIds.Any())
            return BadRequest(new { code = "NO_PROFILE_IDS", message = "At least one profile ID is required." });

        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { code = "REASON_REQUIRED", message = "Reason is required." });

        var adminId = User.GetUserIdFromMetadata();
        var result = await _bulkService.BulkBanAsync(
            request.ProfileIds,
            adminId,
            request.Reason,
            request.DurationDays);

        return Ok(result);
    }

    /// <summary>
    /// Unban multiple users at once
    /// </summary>
    /// <remarks>
    /// Maximum 100 users per request. Users not banned will be skipped.
    /// </remarks>
    [HttpPost("users/unban")]
    [ProducesResponseType(typeof(BulkActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkActionResponse>> BulkUnban([FromBody] BulkUnbanRequest request)
    {
        if (request.ProfileIds == null || !request.ProfileIds.Any())
            return BadRequest(new { code = "NO_PROFILE_IDS", message = "At least one profile ID is required." });

        var adminId = User.GetUserIdFromMetadata();
        var result = await _bulkService.BulkUnbanAsync(
            request.ProfileIds,
            adminId,
            request.Reason);

        return Ok(result);
    }

    /// <summary>
    /// Warn multiple users at once
    /// </summary>
    /// <remarks>
    /// Maximum 100 users per request.
    /// </remarks>
    [HttpPost("users/warn")]
    [ProducesResponseType(typeof(BulkActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkActionResponse>> BulkWarn([FromBody] BulkWarnRequest request)
    {
        if (request.ProfileIds == null || !request.ProfileIds.Any())
            return BadRequest(new { code = "NO_PROFILE_IDS", message = "At least one profile ID is required." });

        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { code = "REASON_REQUIRED", message = "Reason is required." });

        var adminId = User.GetUserIdFromMetadata();
        var result = await _bulkService.BulkWarnAsync(
            request.ProfileIds,
            adminId,
            request.Reason);

        return Ok(result);
    }

    // ============================================================
    // CONTENT MODERATION
    // ============================================================

    /// <summary>
    /// Delete multiple posts at once (soft delete)
    /// </summary>
    /// <remarks>
    /// Maximum 100 posts per request. Already deleted posts will be skipped.
    /// </remarks>
    [HttpPost("posts/delete")]
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
    /// Delete multiple comments at once (hard delete)
    /// </summary>
    /// <remarks>
    /// Maximum 100 comments per request.
    /// </remarks>
    [HttpPost("comments/delete")]
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

    // ============================================================
    // REPORT MANAGEMENT
    // ============================================================

    /// <summary>
    /// Resolve or reject multiple reports at once
    /// </summary>
    /// <remarks>
    /// Maximum 100 reports per request. Only pending reports will be processed.
    /// </remarks>
    [HttpPost("reports/resolve")]
    [ProducesResponseType(typeof(BulkActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkActionResponse>> BulkResolveReports([FromBody] BulkResolveReportsRequest request)
    {
        if (request.ReportIds == null || !request.ReportIds.Any())
            return BadRequest(new { code = "NO_REPORT_IDS", message = "At least one report ID is required." });

        var adminId = User.GetUserIdFromMetadata();
        var result = await _bulkService.BulkResolveReportsAsync(
            request.ReportIds,
            adminId,
            request.NewStatus);

        return Ok(result);
    }
}
