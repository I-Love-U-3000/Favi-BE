using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Favi_BE.ArchitectureTests;

/// <summary>
/// MOD-03: Notifications module consumers must not bypass integration event contracts
/// by directly importing Engagement or Auth application namespaces.
/// Cross-module communication is allowed only through BuildingBlocks (IInbox / IInboxConsumer).
/// NOT-01..NOT-02: CQRS segregation for Commands and Queries namespaces.
/// </summary>
public class NotificationsModuleArchitectureTests
{
    private static readonly Assembly Assembly =
        Favi_BE.Modules.Notifications.AssemblyReference.Assembly;

    private const string ConsumersNs   = "Favi_BE.Modules.Notifications.Application.Consumers";
    private const string CommandsNs    = "Favi_BE.Modules.Notifications.Application.Commands";
    private const string QueriesNs     = "Favi_BE.Modules.Notifications.Application.Queries";
    private const string EngagementApp = "Favi_BE.Modules.Engagement.Application";
    private const string AuthApp       = "Favi_BE.Modules.Auth.Application";

    // NOT-01: Command handlers must not import from the Queries namespace.
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
            because: $"Notifications command handlers must not import from Queries namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // NOT-02: Query handlers must not import from the Commands namespace.
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
            because: $"Notifications query handlers must not import from Commands namespace. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-03a: Notification consumers must not directly call Engagement application code.
    // They receive events through the Inbox (BuildingBlocks); direct coupling breaks isolation.
    [Fact]
    public void NotificationConsumers_Should_Not_Depend_On_EngagementApplication()
    {
        var result = Types
            .InAssembly(Assembly)
            .That()
            .ResideInNamespace(ConsumersNs)
            .ShouldNot()
            .HaveDependencyOn(EngagementApp)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Notification consumers must talk to Engagement only via integration events, " +
                     $"not via direct namespace imports. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // MOD-03b: Notification consumers must not directly call Auth application code.
    [Fact]
    public void NotificationConsumers_Should_Not_Depend_On_AuthApplication()
    {
        var result = Types
            .InAssembly(Assembly)
            .That()
            .ResideInNamespace(ConsumersNs)
            .ShouldNot()
            .HaveDependencyOn(AuthApp)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Notification consumers must not import Auth application namespaces directly. " +
                     $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
