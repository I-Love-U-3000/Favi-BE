using Favi_BE.Modules.Auth.Domain;

namespace Favi_BE.Modules.Auth.Application.Contracts;

public sealed record ProfileInterestsData(string[] Topics, int Version);

public interface IProfileInterestsStore
{
    Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct);
    Task<ProfileInterestsData?> ReadAsync(Guid profileId, CancellationToken ct);
    Task<ProfileInterests?> FindForUpdateAsync(Guid profileId, CancellationToken ct);
    void Add(ProfileInterests interests);
    Task<bool> TrySaveAsync(ProfileInterests interests, CancellationToken ct);
}
