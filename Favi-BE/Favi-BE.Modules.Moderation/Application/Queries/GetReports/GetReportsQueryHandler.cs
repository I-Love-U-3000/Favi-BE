using Favi_BE.Modules.Moderation.Application.Contracts;
using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Queries.GetReports;

internal sealed class GetReportsQueryHandler
    : IRequestHandler<GetReportsQuery, (IReadOnlyList<ReportReadModel> Items, int Total)>
{
    private readonly IModerationQueryReader _reader;

    public GetReportsQueryHandler(IModerationQueryReader reader)
    {
        _reader = reader;
    }

    public Task<(IReadOnlyList<ReportReadModel> Items, int Total)> Handle(
        GetReportsQuery request, CancellationToken cancellationToken)
        => _reader.GetReportsAsync(request.Page, request.PageSize, request.Status, request.TargetType, cancellationToken);
}
