using Favi_BE.Modules.Moderation.Application.Contracts;
using Favi_BE.Modules.Moderation.Application.Contracts.WriteModels;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Commands.LogAdminAction;

internal sealed class LogAdminActionCommandHandler : IRequestHandler<LogAdminActionCommand, Guid>
{
    private readonly IModerationCommandRepository _repo;

    public LogAdminActionCommandHandler(IModerationCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<Guid> Handle(LogAdminActionCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        await _repo.AddAdminActionAsync(new AdminActionWriteData
        {
            Id = id,
            AdminId = request.AdminId,
            ActionType = request.ActionType,
            TargetProfileId = request.TargetProfileId,
            TargetEntityId = request.TargetEntityId,
            TargetEntityType = request.TargetEntityType,
            ReportId = request.ReportId,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _repo.SaveAsync(cancellationToken);

        return id;
    }
}
