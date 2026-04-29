using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using Favi_BE.Modules.Moderation.Domain;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Commands.CreateReport;

public sealed record CreateReportCommand(
    Guid ReporterId,
    ReportTarget TargetType,
    Guid TargetId,
    string? Reason
) : IRequest<ReportReadModel?>;
