using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Favi_BE.ArchitectureTests;

/// <summary>
/// AUTH-01 / AUTH-02: CQRS segregation inside the Auth module.
/// </summary>
public class AuthModuleArchitectureTests
{
    private static readonly Assembly Assembly =
        Favi_BE.Modules.Auth.AssemblyReference.Assembly;

    private const string CommandsNs = "Favi_BE.Modules.Auth.Application.Commands";
    private const string QueriesNs  = "Favi_BE.Modules.Auth.Application.Queries";

    // AUTH-01: Auth command handlers must not cross-import query handlers.
    [Fact]
    public void AuthCommandHandlers_Should_Not_Depend_On_Queries_Namespace()
    {
        var result = Types
            .InAssembly(Assembly)
            .That()
            .ResideInNamespace(CommandsNs)
            .ShouldNot()
            .HaveDependencyOn(QueriesNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Auth command handlers must not import from Queries namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // AUTH-02: Auth query handlers must not cross-import command handlers.
    [Fact]
    public void AuthQueryHandlers_Should_Not_Depend_On_Commands_Namespace()
    {
        var result = Types
            .InAssembly(Assembly)
            .That()
            .ResideInNamespace(QueriesNs)
            .ShouldNot()
            .HaveDependencyOn(CommandsNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Auth query handlers must not import from Commands namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
