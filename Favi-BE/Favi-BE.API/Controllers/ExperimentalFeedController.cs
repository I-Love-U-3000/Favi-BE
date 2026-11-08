using System.ComponentModel.DataAnnotations;
using Favi_BE.Common;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetExperimentalFeed;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

[ApiController]
[Authorize]
public sealed class ExperimentalFeedController(IMediator mediator) : ControllerBase
{
    [HttpGet("api/discovery/experiments/feed")]
    public async Task<IActionResult> Get(
        [FromQuery, RegularExpression("^(ease|ppr|B1|R)$")] string method = "ease",
        [FromQuery, Range(1, 10000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetExperimentalFeedQuery(User.GetUserId(), method, page, pageSize), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
