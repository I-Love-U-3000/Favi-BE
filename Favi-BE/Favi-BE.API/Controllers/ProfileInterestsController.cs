using Favi_BE.Common;
using Favi_BE.Modules.Auth.Application.Commands.SetProfileInterests;
using Favi_BE.Modules.Auth.Application.Queries.GetProfileInterests;
using Favi_BE.Modules.Auth.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

[ApiController]
public sealed class ProfileInterestsController(IMediator mediator) : ControllerBase
{
    [HttpGet("api/discovery/topics")]
    public IActionResult Topics() => Ok(InterestTopics.All);

    [Authorize]
    [HttpGet("api/profiles/me/interests")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var data = await mediator.Send(new GetProfileInterestsQuery(User.GetUserId()), ct);
        return data is null ? NotFound(new { code = "PROFILE_NOT_FOUND" }) : Ok(data);
    }

    public sealed record SetInterestsRequest(string[]? Topics, int Version);

    [Authorize]
    [HttpPut("api/profiles/me/interests")]
    public async Task<IActionResult> Set(SetInterestsRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new SetProfileInterestsCommand(
            User.GetUserId(), request.Topics, request.Version), ct);
        return result.Error switch
        {
            null => Ok(result.Data),
            "PROFILE_NOT_FOUND" => NotFound(new { code = result.Error }),
            "CONCURRENCY_CONFLICT" => Conflict(new { code = result.Error }),
            _ => BadRequest(new { code = result.Error })
        };
    }
}
