using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Favi_BE.ArchitectureTests;

/// <summary>
/// CP-01 through CP-06: CQRS segregation + WriteModels isolation + handler internals + module boundary
/// inside the Content Publishing module.
/// Note: Slice 5 is Commands-only. CP-02 / CP-03 / CP-05 will be empty-pass until Query handlers are added
/// in a later slice — they are present now to enforce the contract from day one.
/// </summary>
public class ContentPublishingModuleArchitectureTests
{
    private static readonly Assembly Assembly =
        Favi_BE.Modules.ContentPublishing.AssemblyReference.Assembly;

    private const string CommandsNs   = "Favi_BE.Modules.ContentPublishing.Application.Commands";
    private const string QueriesNs    = "Favi_BE.Modules.ContentPublishing.Application.Queries";
    private const string WriteModels  = "Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels";

    // CP-01: Command handlers must not import from the Queries namespace.
    [Fact]
    public void CommandHandlers_Should_Not_Depend_On_Queries_Namespace()
    {
        var result = Types
            .InAssembly(Assembly)
            .That()
            .ResideInNamespace(CommandsNs)
            .ShouldNot()
            .HaveDependencyOn(QueriesNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Command handlers must not cross-import from Queries namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // CP-02: Query handlers must not import from the Commands namespace.
    [Fact]
    public void QueryHandlers_Should_Not_Depend_On_Commands_Namespace()
    {
        var result = Types
            .InAssembly(Assembly)
            .That()
            .ResideInNamespace(QueriesNs)
            .ShouldNot()
            .HaveDependencyOn(CommandsNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Query handlers must not import from Commands namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // CP-03: Query handlers must not depend on WriteModels.
    [Fact]
    public void QueryHandlers_Should_Not_Depend_On_WriteModels()
    {
        var result = Types
            .InAssembly(Assembly)
            .That()
            .ResideInNamespace(QueriesNs)
            .ShouldNot()
            .HaveDependencyOn(WriteModels)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Query handlers must not reference WriteModels. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // CP-04: Command handlers must be internal (not public).
    [Fact]
    public void CommandHandlers_Should_Not_Be_Public()
    {
        var result = Types
            .InAssembly(Assembly)
            .That()
            .ResideInNamespace(CommandsNs)
            .And()
            .HaveNameEndingWith("Handler")
            .ShouldNot()
            .BePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Command handlers are internal implementation details and must not be public. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // CP-05: Query handlers must be internal (not public).
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
            because: $"Query handlers are internal implementation details and must not be public. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // CP-06: Content Publishing module must not depend on any other module's application internals.
    [Fact]
    public void ContentPublishing_Should_Not_Depend_On_Other_Module_Application_Namespaces()
    {
        var result = Types
            .InAssembly(Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Favi_BE.Modules.Auth.Application",
                "Favi_BE.Modules.Engagement.Application",
                "Favi_BE.Modules.Notifications.Application",
                "Favi_BE.Modules.SocialGraph.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Content Publishing must not import other modules' application namespaces. " +
                     $"Cross-module interaction must go through outbox/inbox. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
