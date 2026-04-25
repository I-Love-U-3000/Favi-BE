using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Favi_BE.ArchitectureTests;

/// <summary>
/// SG-01 / SG-02 / SG-03 / SG-04: CQRS segregation + WriteModels isolation + handler internals inside Social Graph module.
/// </summary>
public class SocialGraphModuleArchitectureTests
{
    private static readonly Assembly Assembly =
        Favi_BE.Modules.SocialGraph.AssemblyReference.Assembly;

    private const string CommandsNs  = "Favi_BE.Modules.SocialGraph.Application.Commands";
    private const string QueriesNs   = "Favi_BE.Modules.SocialGraph.Application.Queries";
    private const string WriteModels = "Favi_BE.Modules.SocialGraph.Application.Contracts.WriteModels";

    // SG-01: Command handlers must not import from the Queries namespace.
    // Motivation: prevents fat-command anti-pattern where a handler sneaks in read projections.
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

    // SG-02: Query handlers must not import from the Commands namespace.
    // Motivation: queries are read-only projections; importing command logic breaks CQRS contract.
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

    // SG-03: Query handlers must not depend on WriteModels.
    // Motivation: write-side data shapes must stay invisible to read projections.
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

    // SG-04: Command handlers must be internal (not public).
    // Motivation: handlers are module-private implementation details; only Commands/Queries are public contracts.
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

    // SG-05: Query handlers must be internal (not public).
    // Motivation: same as SG-04 — only the query record itself is the public contract.
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

    // SG-06: Social Graph module must not depend on any other module's application internals.
    // Motivation: hard boundary rule — cross-module interaction must go through integration events / outbox-inbox.
    [Fact]
    public void SocialGraph_Should_Not_Depend_On_Other_Module_Application_Namespaces()
    {
        var result = Types
            .InAssembly(Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Favi_BE.Modules.Auth.Application",
                "Favi_BE.Modules.Engagement.Application",
                "Favi_BE.Modules.Notifications.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Social Graph must not import other modules' application namespaces. " +
                     $"Cross-module interaction must go through outbox/inbox. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
