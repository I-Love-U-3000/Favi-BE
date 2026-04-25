using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Favi_BE.ArchitectureTests;

/// <summary>
/// MOD-03: Notifications module consumers must not bypass integration event contracts
/// by directly importing Engagement or Auth application namespaces.
/// Cross-module communication is allowed only through BuildingBlocks (IInbox / IInboxConsumer).
/// </summary>
public class NotificationsModuleArchitectureTests
{
    private static readonly Assembly Assembly =
        Favi_BE.Modules.Notifications.AssemblyReference.Assembly;

    private const string ConsumersNs   = "Favi_BE.Modules.Notifications.Application.Consumers";
    private const string EngagementApp = "Favi_BE.Modules.Engagement.Application";
    private const string AuthApp       = "Favi_BE.Modules.Auth.Application";

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
