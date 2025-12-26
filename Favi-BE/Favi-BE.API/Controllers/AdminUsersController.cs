using Favi_BE.Authorization;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserModerationService _moderationService;

    public AdminUsersController(IUserModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    [HttpPost("{profileId:guid}/ban")]
    public async Task<ActionResult<UserModerationResponse>> Ban(Guid profileId, BanUserRequest request)
    {
        var adminId = User.GetUserIdFromMetadata();
        var response = await _moderationService.BanAsync(profileId, adminId, request);
        if (response is null)
            return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Không tìm thấy hồ sơ." });

        return Ok(response);
    }

    [HttpDelete("{profileId:guid}/ban")]
    public async Task<IActionResult> Unban(Guid profileId, [FromBody] UnbanUserRequest? request = null)
    {
        var adminId = User.GetUserIdFromMetadata();
        var ok = await _moderationService.UnbanAsync(profileId, adminId, request?.Reason);
        if (!ok)
            return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Không tìm thấy hồ sơ hoặc người dùng không bị ban." });

        return Ok(new { message = "Đã gỡ ban người dùng." });
    }

    [HttpPost("{profileId:guid}/warn")]
    public async Task<ActionResult<UserModerationResponse>> Warn(Guid profileId, WarnUserRequest request)
    {
        var adminId = User.GetUserIdFromMetadata();
        var response = await _moderationService.WarnAsync(profileId, adminId, request);
        if (response is null)
            return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Không tìm thấy hồ sơ." });

        return Ok(response);
    }
}
