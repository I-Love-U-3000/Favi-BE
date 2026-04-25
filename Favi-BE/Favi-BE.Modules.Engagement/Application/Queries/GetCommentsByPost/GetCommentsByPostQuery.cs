using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetCommentsByPost;

public sealed record GetCommentsByPostQuery(
    Guid PostId,
    Guid? CurrentUserId,
    int Page,
    int PageSize) : IQuery<(IReadOnlyList<CommentQueryDto> Items, int TotalCount)>;
