using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Auth.Application.Responses;

namespace Favi_BE.Modules.Auth.Application.Queries.GetCurrentUser;

/// <summary>
/// Returns the authenticated user's profile summary.
/// Used by GET /api/auth/me.
/// </summary>
public sealed record GetCurrentUserQuery(Guid ProfileId) : IQuery<CurrentUserDto?>;
