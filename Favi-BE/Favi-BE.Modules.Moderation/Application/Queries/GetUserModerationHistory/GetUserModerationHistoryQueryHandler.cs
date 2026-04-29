using Favi_BE.Modules.Moderation.Application.Contracts;
using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Queries.GetUserModerationHistory;

internal sealed class GetUserModerationHistoryQueryHandler
    : IRequestHandler<GetUserModerationHistoryQuery, (IReadOnlyList<UserModerationReadModel> Items, int Total)>
{
    private readonly IModerationQueryReader _reader;

    public GetUserModerationHistoryQueryHandler(IModerationQueryReader reader)
    {
        _reader = reader;
    }

    public Task<(IReadOnlyList<UserModerationReadModel> Items, int Total)> Handle(
        GetUserModerationHistoryQuery request, CancellationToken cancellationToken)
        => _reader.GetUserModerationHistoryAsync(request.ProfileId, request.Page, request.PageSize, cancellationToken);
}
