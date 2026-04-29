using Favi_BE.Modules.Moderation.Application.Contracts;
using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Queries.GetAdminActionAudit;

internal sealed class GetAdminActionAuditQueryHandler
    : IRequestHandler<GetAdminActionAuditQuery, (IReadOnlyList<AdminActionReadModel> Items, int Total)>
{
    private readonly IModerationQueryReader _reader;

    public GetAdminActionAuditQueryHandler(IModerationQueryReader reader)
    {
        _reader = reader;
    }

    public Task<(IReadOnlyList<AdminActionReadModel> Items, int Total)> Handle(
        GetAdminActionAuditQuery request, CancellationToken cancellationToken)
        => _reader.GetAdminActionAuditAsync(
            request.Page, request.PageSize,
            request.ActionType, request.AdminId, request.TargetProfileId,
            request.FromDate, request.ToDate, request.Search,
            cancellationToken);
}
