using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetCommentById;

public sealed record GetCommentByIdQuery(
    Guid CommentId,
    Guid? CurrentUserId) : IQuery<CommentQueryDto?>;
