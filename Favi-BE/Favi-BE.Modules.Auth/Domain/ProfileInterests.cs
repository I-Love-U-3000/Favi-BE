using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.Modules.Auth.Domain;

public sealed class ProfileInterests : Entity
{
    public Guid Id { get; private set; }
    public string[] Topics { get; private set; } = [];
    public int Version { get; private set; } = 1;

    private ProfileInterests() { }

    public static ProfileInterests Create(Guid profileId, IEnumerable<string> topics)
        => new() { Id = profileId, Topics = InterestTopics.Normalize(topics) };

    public void Replace(IEnumerable<string> topics)
    {
        Topics = InterestTopics.Normalize(topics);
        Version++;
    }
}
