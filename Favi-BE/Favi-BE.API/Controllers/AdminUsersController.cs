using Favi_BE.Authorization;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Modules.Moderation.Application.Commands.ModerateUser;
using Favi_BE.Modules.Moderation.Application.Commands.RevokeModeration;
using Favi_BE.Modules.Moderation.Application.Queries.GetUserModerationHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModDomain = Favi_BE.Modules.Moderation.Domain;
using LegacyModerationActionType = Favi_BE.Models.Enums.ModerationActionType;

namespace Favi_BE.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IBulkActionService _bulkService;

    public AdminUsersController(IMediator mediator, IBulkActionService bulkService)
    {
        _mediator = mediator;
        _bulkService = bulkService;
    }

    [HttpPost("{profileId:guid}/ban")]
    [ProducesResponseType(typeof(UserModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserModerationResponse>> Ban(Guid profileId, BanUserRequest request)
    {
        var adminId = User.GetUserId();
        var result = await _mediator.Send(new ModerateUserCommand(
            profileId, adminId, ModDomain.ModerationActionType.Ban, request.Reason, request.DurationDays));

        if (result is null)
            return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Không tìm thấy hồ sơ." });

        return Ok(MapModeration(result));
    }

    [HttpDelete("{profileId:guid}/ban")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unban(Guid profileId, [FromBody] UnbanUserRequest? request = null)
    {
        var adminId = User.GetUserId();
        var result = await _mediator.Send(new RevokeModerationCommand(profileId, adminId, request?.Reason));

        if (!result.Succeeded)
            return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Không tìm thấy hồ sơ hoặc người dùng không bị ban." });

        return Ok(new { message = "Đã gỡ ban người dùng." });
    }

    [HttpPost("{profileId:guid}/warn")]
    [ProducesResponseType(typeof(UserModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserModerationResponse>> Warn(Guid profileId, WarnUserRequest request)
    {
        var adminId = User.GetUserId();
        var result = await _mediator.Send(new ModerateUserCommand(
            profileId, adminId, ModDomain.ModerationActionType.Warn, request.Reason, null));

        if (result is null)
            return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Không tìm thấy hồ sơ." });

        return Ok(MapModeration(result));
    }

    [HttpGet("{profileId:guid}/warnings")]
    [ProducesResponseType(typeof(UserWarningsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserWarningsResponse>> GetWarnings(
        Guid profileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _mediator.Send(new GetUserModerationHistoryQuery(profileId, page, pageSize));
        var warnings = items.Where(m => m.ActionType == ModDomain.ModerationActionType.Warn)
            .Select(MapModeration).ToList();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new UserWarningsResponse(warnings, total, page, pageSize, totalPages));
    }

    [HttpGet("{profileId:guid}/ban-history")]
    [ProducesResponseType(typeof(UserBanHistoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserBanHistoryResponse>> GetBanHistory(
        Guid profileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _mediator.Send(new GetUserModerationHistoryQuery(profileId, page, pageSize));
        var bans = items.Where(m => m.ActionType == ModDomain.ModerationActionType.Ban)
            .Select(MapModeration).ToList();
        var activeBan = bans.FirstOrDefault(b => b.Active);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new UserBanHistoryResponse(bans, total, page, pageSize, totalPages, activeBan));
    }

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
        var result = await _bulkService.BulkBanAsync(request.ProfileIds, adminId, request.Reason, request.DurationDays);
        return Ok(result);
    }

    [HttpPost("bulk/unban")]
    [ProducesResponseType(typeof(BulkActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkActionResponse>> BulkUnban([FromBody] BulkUnbanRequest request)
    {
        if (request.ProfileIds == null || !request.ProfileIds.Any())
            return BadRequest(new { code = "NO_PROFILE_IDS", message = "At least one profile ID is required." });

        var adminId = User.GetUserIdFromMetadata();
        var result = await _bulkService.BulkUnbanAsync(request.ProfileIds, adminId, request.Reason);
        return Ok(result);
    }

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
        var result = await _bulkService.BulkWarnAsync(request.ProfileIds, adminId, request.Reason);
        return Ok(result);
    }

    private static UserModerationResponse MapModeration(
        Favi_BE.Modules.Moderation.Application.Contracts.ReadModels.UserModerationReadModel m) =>
        new(m.Id, m.ProfileId,
            (LegacyModerationActionType)(int)m.ActionType,
            m.Reason, m.CreatedAt, m.ExpiresAt, m.RevokedAt,
            m.Active, m.AdminActionId, m.AdminId);
}
