using Favi_BE.Data;
using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Favi_BE.API.Application.Auth;

internal sealed class ProfileInterestsStore(AppDbContext db) : IProfileInterestsStore
{
    public Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct)
        => db.Profiles.AsNoTracking().AnyAsync(p => p.Id == profileId, ct);

    public Task<ProfileInterestsData?> ReadAsync(Guid profileId, CancellationToken ct)
        => db.ProfileInterests.AsNoTracking().Where(p => p.Id == profileId)
            .Select(p => new ProfileInterestsData(p.Topics, p.Version)).SingleOrDefaultAsync(ct);

    public Task<ProfileInterests?> FindForUpdateAsync(Guid profileId, CancellationToken ct)
        => db.ProfileInterests.SingleOrDefaultAsync(p => p.Id == profileId, ct);

    public void Add(ProfileInterests interests) => db.ProfileInterests.Add(interests);

    public async Task<bool> TrySaveAsync(ProfileInterests interests, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); return true; }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(interests).State = EntityState.Detached;
            return false;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        {
            // Concurrent first insert (expected version 0); do not overwrite the winner.
            db.Entry(interests).State = EntityState.Detached;
            return false;
        }
    }
}
