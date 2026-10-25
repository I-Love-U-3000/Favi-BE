using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Domain;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.SetProfileInterests;

public sealed record SetProfileInterestsCommand(Guid ProfileId, string[]? Topics, int Version)
    : ICommand<SetProfileInterestsResult>;

public sealed record SetProfileInterestsResult(ProfileInterestsData? Data, string? Error);

internal sealed class SetProfileInterestsCommandHandler(IProfileInterestsStore store)
    : IRequestHandler<SetProfileInterestsCommand, SetProfileInterestsResult>
{
    public async Task<SetProfileInterestsResult> Handle(SetProfileInterestsCommand request, CancellationToken ct)
    {
        string[] topics;
        try { topics = InterestTopics.Normalize(request.Topics); }
        catch (ArgumentException) { return new(null, "INVALID_TOPICS"); }
        if (request.Version < 0) return new(null, "INVALID_VERSION");
        if (!await store.ProfileExistsAsync(request.ProfileId, ct)) return new(null, "PROFILE_NOT_FOUND");
        var interests = await store.FindForUpdateAsync(request.ProfileId, ct);
        if (request.Version != (interests?.Version ?? 0)) return new(null, "CONCURRENCY_CONFLICT");
        if (interests is null)
        {
            interests = ProfileInterests.Create(request.ProfileId, topics);
            store.Add(interests);
        }
        else interests.Replace(topics);
        return await store.TrySaveAsync(interests, ct)
            ? new(new(interests.Topics.ToArray(), interests.Version), null)
            : new(null, "CONCURRENCY_CONFLICT");
    }
}
