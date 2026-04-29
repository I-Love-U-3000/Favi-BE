using Favi_BE.Modules.Moderation.Application.Contracts;
using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Queries.GetReportById;

internal sealed class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, ReportReadModel?>
{
    private readonly IModerationQueryReader _reader;

    public GetReportByIdQueryHandler(IModerationQueryReader reader)
    {
        _reader = reader;
    }

    public Task<ReportReadModel?> Handle(GetReportByIdQuery request, CancellationToken cancellationToken)
        => _reader.GetReportByIdAsync(request.ReportId, cancellationToken);
}
