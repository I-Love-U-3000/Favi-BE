using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using Favi_BE.Modules.Moderation.Domain;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Queries.GetReports;

public sealed record GetReportsQuery(
    int Page,
    int PageSize,
    ReportStatus? Status,
    ReportTarget? TargetType,
    Guid? ReporterId
) : IRequest<(IReadOnlyList<ReportReadModel> Items, int Total)>;
