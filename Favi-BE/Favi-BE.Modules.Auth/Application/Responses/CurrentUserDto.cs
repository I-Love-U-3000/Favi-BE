namespace Favi_BE.Modules.Auth.Application.Responses;

public sealed record CurrentUserDto(
    Guid Id,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    string Role);
