using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Queries.GetReportById;

public sealed record GetReportByIdQuery(Guid ReportId) : IRequest<ReportReadModel?>;
