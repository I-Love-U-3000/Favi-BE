using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Queries.GetUserModerationHistory;

public sealed record GetUserModerationHistoryQuery(
    Guid ProfileId,
    int Page,
    int PageSize
) : IRequest<(IReadOnlyList<UserModerationReadModel> Items, int Total)>;
