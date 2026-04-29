using Favi_BE.Modules.Moderation.Application.Contracts;
using Favi_BE.Modules.Moderation.Application.Contracts.WriteModels;
using Favi_BE.Modules.Moderation.Application.Responses;
using Favi_BE.Modules.Moderation.Domain;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Commands.ResolveReport;

internal sealed class ResolveReportCommandHandler : IRequestHandler<ResolveReportCommand, ModerationCommandResult>
{
    private readonly IModerationCommandRepository _repo;

    public ResolveReportCommandHandler(IModerationCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<ModerationCommandResult> Handle(ResolveReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _repo.GetReportAsync(request.ReportId, cancellationToken);
        if (report is null)
            return ModerationCommandResult.Fail("REPORT_NOT_FOUND");

        var now = DateTime.UtcNow;

        await _repo.UpdateReportStatusAsync(request.ReportId, request.Resolution, now, cancellationToken);

        var adminAction = new AdminActionWriteData
        {
            Id = Guid.NewGuid(),
            AdminId = request.AdminId,
            ActionType = AdminActionType.ResolveReport,
            ReportId = request.ReportId,
            Notes = request.Notes,
            CreatedAt = now
        };

        await _repo.AddAdminActionAsync(adminAction, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return ModerationCommandResult.Success();
    }
}
