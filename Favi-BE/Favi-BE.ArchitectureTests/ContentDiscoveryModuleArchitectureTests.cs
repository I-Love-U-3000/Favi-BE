using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Favi_BE.ArchitectureTests;

/// <summary>
/// CD-01..CD-05: CQRS segregation, handler internals, and cross-module boundary enforcement
/// for the ContentDiscovery module (pure read context — no Commands namespace).
/// </summary>
public class ContentDiscoveryModuleArchitectureTests
{
    private static readonly Assembly Assembly =
        Favi_BE.Modules.ContentDiscovery.AssemblyReference.Assembly;

    private const string QueriesNs = "Favi_BE.Modules.ContentDiscovery.Application.Queries";

    // CD-01: Query handlers must not import from ContentPublishing Commands namespace.
    [Fact]
    public void QueryHandlers_Should_Not_Depend_On_ContentPublishing_Commands()
    {
        var result = Types
            .InAssembly(Assembly)
            .That()
            .ResideInNamespace(QueriesNs)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.ContentPublishing.Application.Commands")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"ContentDiscovery query handlers must not import ContentPublishing commands. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // CD-02: Query handlers must not import from ContentPublishing WriteModels.
    [Fact]
    public void QueryHandlers_Should_Not_Depend_On_ContentPublishing_WriteModels()
    {
        var result = Types
            .InAssembly(Assembly)
            .That()
            .ResideInNamespace(QueriesNs)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"ContentDiscovery query handlers must not import ContentPublishing write models. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // CD-03: Query handlers must be internal (not public).
    [Fact]
    public void QueryHandlers_Should_Not_Be_Public()
    {
        var result = Types
            .InAssembly(Assembly)
            .That()
            .ResideInNamespace(QueriesNs)
            .And()
            .HaveNameEndingWith("Handler")
            .ShouldNot()
            .BePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"ContentDiscovery query handlers are internal implementation details and must not be public. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // CD-04: ContentDiscovery module must not depend on other module application internals
    // except via the declared port (IContentDiscoveryQueryReader).
    [Fact]
    public void ContentDiscovery_Should_Not_Depend_On_Other_Module_Application_Namespaces()
    {
        var result = Types
            .InAssembly(Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Favi_BE.Modules.Auth.Application",
                "Favi_BE.Modules.Engagement.Application",
                "Favi_BE.Modules.Notifications.Application",
                "Favi_BE.Modules.SocialGraph.Application",
                "Favi_BE.Modules.Messaging.Application",
                "Favi_BE.Modules.Moderation.Application",
                "Favi_BE.Modules.Stories.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"ContentDiscovery must not import other modules' application namespaces. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // CD-05: ContentDiscovery must not import ContentPublishing Domain types directly
    // (read context must remain decoupled from the write domain model).
    [Fact]
    public void ContentDiscovery_Should_Not_Depend_On_ContentPublishing_Domain()
    {
        var result = Types
            .InAssembly(Assembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.ContentPublishing.Domain")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"ContentDiscovery (read context) must not depend on ContentPublishing domain types. " +
                     $"Use int primitives for privacy/status fields in ReadModels. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
