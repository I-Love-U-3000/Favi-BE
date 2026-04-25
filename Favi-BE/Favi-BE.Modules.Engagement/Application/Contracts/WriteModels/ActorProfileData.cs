namespace Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;

public sealed record ActorProfileData(
    Guid Id,
    string Username,
    string? DisplayName,
    string? AvatarUrl);
