using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.Modules.Stories.Domain.Rules;

public sealed class StoryTTL24hRule : IBusinessRule
{
    private readonly DateTime _createdAt;
    private readonly DateTime _expiresAt;

    public StoryTTL24hRule(DateTime createdAt, DateTime expiresAt)
    {
        _createdAt = createdAt;
        _expiresAt = expiresAt;
    }

    public bool IsBroken() => Math.Abs((_expiresAt - _createdAt).TotalHours - 24.0) > 0.01;

    public string Message => "Story expiration time must be set to exactly 24 hours after creation.";
}
