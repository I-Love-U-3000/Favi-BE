using Favi_BE.Data;
using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Application.Auth;

/// <summary>
/// Implements IAuthQueryReader (Auth module port) using AppDbContext AsNoTracking.
/// No mutations allowed — pure read side.
/// </summary>
internal sealed class AuthQueryReaderAdapter : IAuthQueryReader
{
    private readonly AppDbContext _db;

    public AuthQueryReaderAdapter(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await _db.Profiles
            .AsNoTracking()
            .Where(p => p.Id == profileId)
            .Select(p => new CurrentUserDto(
                p.Id,
                p.Username,
                p.DisplayName,
                p.AvatarUrl,
                p.Role.ToString().ToLower()))
            .FirstOrDefaultAsync(ct);

        return profile;
    }
}
