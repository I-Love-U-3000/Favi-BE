using Favi_BE.Authorization;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

/// <summary>
/// Admin endpoints for comment management
/// </summary>
[ApiController]
[Route("api/admin/comments")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminCommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public AdminCommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    /// <summary>
    /// Get all comments with pagination and filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AnalyticsCommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnalyticsCommentDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? postId,
        [FromQuery] Guid? authorId,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? page,
        [FromQuery] int? take,
        [FromQuery] int? skip,
        [FromQuery] int? pageSize)
    {
        // Handle pagination flexibly
        int finalPageSize = take ?? pageSize ?? 20;
        int finalPage = page ?? (skip.HasValue ? (skip.Value / finalPageSize) + 1 : 1);

        if (finalPage < 1) finalPage = 1;
        if (finalPageSize < 1) finalPageSize = 20;
        if (finalPageSize > 100) finalPageSize = 100;

        var result = await _commentService.GetAllAsync(
            search, 
            postId, 
            authorId, 
            status, 
            startDate, 
            endDate, 
            finalPage, 
            finalPageSize);

        return Ok(result);
    }

    /// <summary>
    /// Get a single comment by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentResponse>> GetById(Guid id)
    {
        var comment = await _commentService.GetByIdAsync(id, null);
        if (comment == null)
            return NotFound(new { code = "COMMENT_NOT_FOUND", message = "Không tìm thấy bình luận." });

        return Ok(comment);
    }
    
    /// <summary>
    /// Get comment statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CommentStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetStats()
    {
        var stats = await _commentService.GetStatsAsync();
        return Ok(new
        {
            total = stats.Total,
            deleted = stats.Deleted,
            hidden = stats.Hidden,
            active = stats.Active
        });
    }
}
