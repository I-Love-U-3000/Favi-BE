namespace Favi_BE.Modules.Stories.Application.Contracts.ReadModels;

public sealed record StoryViewerReadModel(
    Guid ViewerId,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    DateTime ViewedAt);
