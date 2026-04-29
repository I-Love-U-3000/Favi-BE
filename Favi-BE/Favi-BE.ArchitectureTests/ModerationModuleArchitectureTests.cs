using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Favi_BE.ArchitectureTests;

/// <summary>
/// MOD-01..MOD-06: CQRS segregation, WriteModels isolation, handler internals,
/// and cross-module boundary enforcement for the Moderation module.
/// </summary>
public class ModerationModuleArchitectureTests
{
    private static readonly Assembly Assembly =
        Favi_BE.Modules.Moderation.AssemblyReference.Assembly;

    private const string CommandsNs  = "Favi_BE.Modules.Moderation.Application.Commands";
    private const string QueriesNs   = "Favi_BE.Modules.Moderation.Application.Queries";
    private const string WriteModels = "Favi_BE.Modules.Moderation.Application.Contracts.WriteModels";

    // MOD-01: Command handlers must not import from the Queries namespace.
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

    // MOD-02: Query handlers must not import from the Commands namespace.
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

    // MOD-03: Query handlers must not depend on WriteModels.
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

    // MOD-04: Command handlers must be internal.
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

    // MOD-05: Query handlers must be internal.
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

    // MOD-06: Moderation module must not depend on any other module's application internals.
    [Fact]
    public void Moderation_Should_Not_Depend_On_Other_Module_Application_Namespaces()
    {
        var result = Types
            .InAssembly(Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Favi_BE.Modules.Auth.Application",
                "Favi_BE.Modules.Engagement.Application",
                "Favi_BE.Modules.Notifications.Application",
                "Favi_BE.Modules.SocialGraph.Application",
                "Favi_BE.Modules.ContentPublishing.Application",
                "Favi_BE.Modules.Stories.Application",
                "Favi_BE.Modules.Messaging.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Moderation must not import other modules' application namespaces. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
