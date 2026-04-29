using Favi_BE.Modules.Moderation.Application.Responses;
using Favi_BE.Modules.Moderation.Domain;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Commands.ResolveReport;

public sealed record ResolveReportCommand(
    Guid ReportId,
    Guid AdminId,
    ReportStatus Resolution,
    string? Notes
) : IRequest<ModerationCommandResult>;
