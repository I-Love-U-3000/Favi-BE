using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using Favi_BE.Modules.Moderation.Domain;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Queries.GetAdminActionAudit;

public sealed record GetAdminActionAuditQuery(
    int Page,
    int PageSize,
    AdminActionType? ActionType,
    Guid? AdminId,
    Guid? TargetProfileId,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Search
) : IRequest<(IReadOnlyList<AdminActionReadModel> Items, int Total)>;
