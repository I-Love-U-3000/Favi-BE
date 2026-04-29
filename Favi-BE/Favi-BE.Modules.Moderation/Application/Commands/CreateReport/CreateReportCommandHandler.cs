using Favi_BE.Modules.Moderation.Application.Contracts;
using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using Favi_BE.Modules.Moderation.Application.Contracts.WriteModels;
using Favi_BE.Modules.Moderation.Domain;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Commands.CreateReport;

internal sealed class CreateReportCommandHandler : IRequestHandler<CreateReportCommand, ReportReadModel?>
{
    private readonly IModerationCommandRepository _repo;

    public CreateReportCommandHandler(IModerationCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<ReportReadModel?> Handle(CreateReportCommand request, CancellationToken cancellationToken)
    {
        if (!await _repo.ProfileExistsAsync(request.ReporterId, cancellationToken))
            return null;

        var data = new ReportWriteData
        {
            Id = Guid.NewGuid(),
            ReporterId = request.ReporterId,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            Reason = request.Reason,
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddReportAsync(data, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return new ReportReadModel(
            data.Id,
            data.ReporterId,
            data.TargetType,
            data.TargetId,
            data.Reason,
            data.Status,
            data.CreatedAt,
            null,
            null);
    }
}
