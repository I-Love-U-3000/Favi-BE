using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Favi_BE.ArchitectureTests;

/// <summary>
/// API-01 + MOD-01/02: Controllers must route through MediatR, not raw repositories.
/// Module assemblies must not reference each other's internal application namespaces.
/// </summary>
public class ApiLayerArchitectureTests
{
    private static readonly Assembly ApiAssembly               = Favi_BE.API.AssemblyReference.Assembly;
    private static readonly Assembly AuthAssembly              = Favi_BE.Modules.Auth.AssemblyReference.Assembly;
    private static readonly Assembly EngagementAssembly        = Favi_BE.Modules.Engagement.AssemblyReference.Assembly;
    private static readonly Assembly NotificationsAssembly     = Favi_BE.Modules.Notifications.AssemblyReference.Assembly;
    private static readonly Assembly SocialGraphAssembly       = Favi_BE.Modules.SocialGraph.AssemblyReference.Assembly;
    private static readonly Assembly ContentPublishingAssembly = Favi_BE.Modules.ContentPublishing.AssemblyReference.Assembly;

    // -- API Layer --

    // API-01: Controllers must not directly reference the repository layer.
    // Motivation: all persistence access must flow through IMediator (Commands/Queries) or
    // through legacy IService interfaces — never raw repositories injected into controllers.
    // NOTE: Adapters in Favi_BE.API.Application.* are exempt by design; this rule scopes
    //       to Favi_BE.Controllers only.
    [Fact]
    public void Controllers_Should_Not_Depend_On_Repositories_Directly()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("Favi_BE.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.API.Data.Repositories")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Controllers must call IMediator or IService, not raw repositories. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // -- Module Boundary Isolation --

    // MOD-01a: Engagement module must not know about Auth application internals.
    [Fact]
    public void Engagement_Should_Not_Depend_On_AuthApplication()
    {
        var result = Types
            .InAssembly(EngagementAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Auth.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Engagement must not import Auth application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-01b: Engagement module must not know about Notifications application internals.
    [Fact]
    public void Engagement_Should_Not_Depend_On_NotificationsApplication()
    {
        var result = Types
            .InAssembly(EngagementAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Notifications.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Engagement must not import Notifications application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-02a: Auth module must not know about Engagement application internals.
    [Fact]
    public void Auth_Should_Not_Depend_On_EngagementApplication()
    {
        var result = Types
            .InAssembly(AuthAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Engagement.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Auth must not import Engagement application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-02b: Auth module must not know about Notifications application internals.
    [Fact]
    public void Auth_Should_Not_Depend_On_NotificationsApplication()
    {
        var result = Types
            .InAssembly(AuthAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Notifications.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Auth must not import Notifications application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-03a: Social Graph module must not know about Engagement application internals.
    [Fact]
    public void SocialGraph_Should_Not_Depend_On_EngagementApplication()
    {
        var result = Types
            .InAssembly(SocialGraphAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Engagement.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Social Graph must not import Engagement application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-03b: Social Graph module must not know about Auth application internals.
    [Fact]
    public void SocialGraph_Should_Not_Depend_On_AuthApplication()
    {
        var result = Types
            .InAssembly(SocialGraphAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Auth.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Social Graph must not import Auth application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-03c: Social Graph module must not know about Notifications application internals.
    [Fact]
    public void SocialGraph_Should_Not_Depend_On_NotificationsApplication()
    {
        var result = Types
            .InAssembly(SocialGraphAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Notifications.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Social Graph must not import Notifications application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-04: Engagement module must not know about Social Graph application internals.
    [Fact]
    public void Engagement_Should_Not_Depend_On_SocialGraphApplication()
    {
        var result = Types
            .InAssembly(EngagementAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.SocialGraph.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Engagement must not import Social Graph application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-05a: Content Publishing must not know about Auth application internals.
    [Fact]
    public void ContentPublishing_Should_Not_Depend_On_AuthApplication()
    {
        var result = Types
            .InAssembly(ContentPublishingAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Auth.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Content Publishing must not import Auth application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-05b: Content Publishing must not know about Engagement application internals.
    [Fact]
    public void ContentPublishing_Should_Not_Depend_On_EngagementApplication()
    {
        var result = Types
            .InAssembly(ContentPublishingAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Engagement.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Content Publishing must not import Engagement application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-05c: Content Publishing must not know about Notifications application internals.
    [Fact]
    public void ContentPublishing_Should_Not_Depend_On_NotificationsApplication()
    {
        var result = Types
            .InAssembly(ContentPublishingAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.Notifications.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Content Publishing must not import Notifications application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-05d: Content Publishing must not know about Social Graph application internals.
    [Fact]
    public void ContentPublishing_Should_Not_Depend_On_SocialGraphApplication()
    {
        var result = Types
            .InAssembly(ContentPublishingAssembly)
            .ShouldNot()
            .HaveDependencyOn("Favi_BE.Modules.SocialGraph.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Content Publishing must not import Social Graph application namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
