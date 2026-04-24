using Favi_BE.Modules.Auth.Application.Responses;

namespace Favi_BE.Modules.Auth.Application.Contracts;

/// <summary>
/// Read-side port for the Auth module.
/// Query handlers use this for AsNoTracking reads. No mutations allowed.
/// </summary>
public interface IAuthQueryReader
{
    Task<CurrentUserDto?> GetCurrentUserAsync(Guid profileId, CancellationToken ct = default);
}
