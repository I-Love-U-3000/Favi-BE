using Favi_BE.Modules.Engagement.Domain;

namespace Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

public sealed record ReactorQueryDto(
    Guid ProfileId,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    ReactionType ReactionType,
    DateTime ReactedAt);
