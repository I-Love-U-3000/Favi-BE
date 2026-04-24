using Favi_BE.Common;
using Favi_BE.Models.Dtos;
using Favi_BE.Modules.Auth.Application.Commands.ChangePassword;
using Favi_BE.Modules.Auth.Application.Commands.Login;
using Favi_BE.Modules.Auth.Application.Commands.Logout;
using Favi_BE.Modules.Auth.Application.Commands.RefreshToken;
using Favi_BE.Modules.Auth.Application.Commands.Register;
using Favi_BE.Modules.Auth.Application.Queries.GetCurrentUser;
using Favi_BE.Modules.Auth.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public record LoginDto(string EmailOrUsername, string Password);
    public record RegisterDto(string Email, string Password, string Username, string? DisplayName);
    public record ChangePasswordDto(string CurrentPassword, string NewPassword);

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginDto dto)
    {
        var result = await _mediator.Send(new LoginCommand(dto.EmailOrUsername, dto.Password));
        if (!result.IsSuccess)
        {
            return result.Error!.Code switch
            {
                "USER_BANNED" => StatusCode(403, new { code = result.Error.Code, message = result.Error.Message }),
                _ => Unauthorized(new { code = result.Error!.Code, message = result.Error.Message })
            };
        }
        return Ok(new AuthResponse(result.AccessToken!, result.RefreshToken!, result.Message!));
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterDto dto)
    {
        var result = await _mediator.Send(new RegisterCommand(dto.Email, dto.Password, dto.Username, dto.DisplayName));
        if (!result.IsSuccess)
        {
            return result.Error!.Code switch
            {
                "USERNAME_EXISTS" => Conflict(new { code = result.Error.Code, message = result.Error.Message }),
                "EMAIL_EXISTS" => Conflict(new { code = result.Error.Code, message = result.Error.Message }),
                _ => BadRequest(new { code = result.Error!.Code, message = result.Error.Message })
            };
        }
        return Ok(new AuthResponse(result.AccessToken!, result.RefreshToken!, result.Message!));
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] string refreshToken)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(refreshToken));
        if (!result.IsSuccess)
            return Unauthorized(new { code = result.Error!.Code, message = result.Error.Message });

        return Ok(new AuthResponse(result.AccessToken!, result.RefreshToken!, result.Message!));
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] string? refreshToken = null)
    {
        var result = await _mediator.Send(new LogoutCommand(refreshToken));
        return Ok(new { message = result.Message });
    }

    // POST /api/auth/change-password
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var profileId = User.GetUserId();
        var result = await _mediator.Send(new ChangePasswordCommand(profileId, dto.CurrentPassword, dto.NewPassword));
        if (!result.IsSuccess)
            return BadRequest(new { code = result.Error!.Code, message = result.Error.Message });

        return Ok(new { message = result.Message });
    }

    // GET /api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> GetCurrentUser()
    {
        var profileId = User.GetUserId();
        var result = await _mediator.Send(new GetCurrentUserQuery(profileId));
        if (result is null)
            return NotFound(new { code = "USER_NOT_FOUND", message = "Không tìm thấy thông tin người dùng." });

        return Ok(result);
    }
}
