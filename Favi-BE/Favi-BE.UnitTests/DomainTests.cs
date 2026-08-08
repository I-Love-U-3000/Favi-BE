using Xunit;
using FluentAssertions;
using Favi_BE.BuildingBlocks.Domain;
using Favi_BE.Modules.Auth.Domain;
using Favi_BE.Modules.SocialGraph.Domain;
using Favi_BE.Modules.SocialGraph.Domain.Rules;
using Favi_BE.Modules.ContentPublishing.Domain;
using Favi_BE.Modules.ContentPublishing.Domain.Rules;
using Favi_BE.Modules.Stories.Domain;
using Favi_BE.Modules.Stories.Domain.Rules;

namespace Favi_BE.UnitTests;

public class DomainTests
{
    [Fact]
    public void Profile_Create_Should_Initialize_Properties()
    {
        var id = Guid.NewGuid();
        var profile = Profile.Create(id, "john_doe", "User", DateTime.UtcNow);

        profile.Id.Should().Be(id);
        profile.Username.Should().Be("john_doe");
        profile.Role.Should().Be("User");
        profile.IsBanned.Should().BeFalse();
    }

    [Fact]
    public void FollowRelationship_Create_With_Self_Should_Throw_BusinessRuleValidationException()
    {
        var id = Guid.NewGuid();
        
        var act = () => FollowRelationship.Create(id, id);

        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("You cannot follow your own profile.");
    }

    [Fact]
    public void FollowRelationship_Create_With_Different_Profiles_Should_Succeed()
    {
        var followerId = Guid.NewGuid();
        var followeeId = Guid.NewGuid();

        var relation = FollowRelationship.Create(followerId, followeeId);

        relation.FollowerId.Should().Be(followerId);
        relation.FolloweeId.Should().Be(followeeId);
    }

    [Fact]
    public void Post_Update_By_Non_Owner_Should_Throw_BusinessRuleValidationException()
    {
        var ownerId = Guid.NewGuid();
        var nonOwnerId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        var post = Post.Create(postId, ownerId, "Hello", PostPrivacy.Public, null, null, null, null);

        var act = () => post.Update(nonOwnerId, "New caption", PostPrivacy.Private);

        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("You are not authorized to perform actions on this content.");
    }

    [Fact]
    public void Story_Create_With_Non_24h_TTL_Should_Throw_BusinessRuleValidationException()
    {
        var profileId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var act = () => Story.Create(
            id, profileId, "http://media.com", "pub123", 100, 100, "jpg", null,
            StoryPrivacy.Public, now, now.AddHours(23)); // 23 hours instead of 24 hours

        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("Story expiration time must be set to exactly 24 hours after creation.");
    }
}
