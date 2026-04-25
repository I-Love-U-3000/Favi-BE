using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Favi_BE.ArchitectureTests;

/// <summary>
/// ENG-01 / ENG-02 / ENG-03: CQRS segregation + WriteModels isolation inside Engagement module.
/// </summary>
public class EngagementModuleArchitectureTests
{
    private static readonly Assembly Assembly =
        Favi_BE.Modules.Engagement.AssemblyReference.Assembly;

    private const string CommandsNs  = "Favi_BE.Modules.Engagement.Application.Commands";
    private const string QueriesNs   = "Favi_BE.Modules.Engagement.Application.Queries";
    private const string WriteModels = "Favi_BE.Modules.Engagement.Application.Contracts.WriteModels";

    // ENG-01: Command handlers must not import from the Queries namespace.
    // Motivation: prevents handlers from sneaking in query logic (fat-command anti-pattern).
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

    // ENG-02: Query handlers must not import from the Commands namespace.
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

    // ENG-03: Query handlers must not depend on WriteModels.
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
}
