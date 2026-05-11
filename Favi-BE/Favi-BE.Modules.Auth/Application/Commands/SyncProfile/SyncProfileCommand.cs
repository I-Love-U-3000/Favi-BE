using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.SyncProfile;

/// <summary>Idempotent upsert from Supabase webhook. Returns true if profile was created, false if already existed.</summary>
public sealed record SyncProfileCommand(
    Guid UserId,
    string Username,
    string DisplayName
) : IRequest<bool>;
