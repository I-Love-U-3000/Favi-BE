using Favi_BE.Authorization;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

/// <summary>
/// Admin endpoints for user management
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminUsersController : ControllerBase
{
    private readonly IUserModerationService _moderationService;
    private readonly IBulkActionService _bulkService;

    public AdminUsersController(
        IUserModerationService moderationService,
        IBulkActionService bulkService)
    {
        _moderationService = moderationService;
        _bulkService = bulkService;
    }

    // ============================================================
    // SINGLE USER ACTIONS
    // ============================================================

    /// <summary>
    /// Ban a single user
    /// </summary>
    [HttpPost("{profileId:guid}/ban")]
    [ProducesResponseType(typeof(UserModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserModerationResponse>> Ban(Guid profileId, BanUserRequest request)
    {
        var adminId = User.GetUserIdFromMetadata();
        var response = await _moderationService.BanAsync(profileId, adminId, request);
        if (response is null)
            return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Không tìm thấy hồ sơ." });

        return Ok(response);
    }

    /// <summary>
    /// Unban a single user
    /// </summary>
    [HttpDelete("{profileId:guid}/ban")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unban(Guid profileId, [FromBody] UnbanUserRequest? request = null)
    {
        var adminId = User.GetUserIdFromMetadata();
        var ok = await _moderationService.UnbanAsync(profileId, adminId, request?.Reason);
        if (!ok)
            return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Không tìm thấy hồ sơ hoặc người dùng không bị ban." });

        return Ok(new { message = "Đã gỡ ban người dùng." });
    }

    /// <summary>
    /// Warn a single user
    /// </summary>
    [HttpPost("{profileId:guid}/warn")]
    [ProducesResponseType(typeof(UserModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserModerationResponse>> Warn(Guid profileId, WarnUserRequest request)
    {
        var adminId = User.GetUserIdFromMetadata();
        var response = await _moderationService.WarnAsync(profileId, adminId, request);
        if (response is null)
            return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Không tìm thấy hồ sơ." });

        return Ok(response);
    }

    // ============================================================
    // BULK USER ACTIONS
    // ============================================================

    /// <summary>
    /// Ban multiple users at once (max 100)
    /// </summary>
    [HttpPost("bulk/ban")]
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
    /// Unban multiple users at once (max 100)
    /// </summary>
    [HttpPost("bulk/unban")]
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
    /// Warn multiple users at once (max 100)
    /// </summary>
    [HttpPost("bulk/warn")]
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
}
