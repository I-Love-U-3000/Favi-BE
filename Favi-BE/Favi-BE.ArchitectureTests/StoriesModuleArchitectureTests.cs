using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Favi_BE.ArchitectureTests;

/// <summary>
/// MOD-06: Stories module must not depend on any other module's application internals.
/// </summary>
public class StoriesModuleArchitectureTests
{
    private static readonly Assembly StoriesAssembly = Favi_BE.Modules.Stories.AssemblyReference.Assembly;

    [Fact]
    public void Stories_Should_Not_Depend_On_AuthApplication()
    {
        var result = Types
            .InAssembly(StoriesAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Auth.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Stories must not import Auth application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Stories_Should_Not_Depend_On_EngagementApplication()
    {
        var result = Types
            .InAssembly(StoriesAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Engagement.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Stories must not import Engagement application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Stories_Should_Not_Depend_On_NotificationsApplication()
    {
        var result = Types
            .InAssembly(StoriesAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Notifications.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Stories must not import Notifications application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Stories_Should_Not_Depend_On_SocialGraphApplication()
    {
        var result = Types
            .InAssembly(StoriesAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.SocialGraph.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Stories must not import Social Graph application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Stories_Should_Not_Depend_On_ContentPublishingApplication()
    {
        var result = Types
            .InAssembly(StoriesAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.ContentPublishing.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Stories must not import Content Publishing application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
