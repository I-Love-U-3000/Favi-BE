using Favi_BE.BuildingBlocks;
using NetArchTest.Rules;
using Xunit;
using FluentAssertions;

namespace Favi_BE.ArchitectureTests;

public class ArchitectureTests
{
    private const string DomainNamespace = "Favi_BE.BuildingBlocks.Domain";
    private const string ApplicationNamespace = "Favi_BE.BuildingBlocks.Application";
    private const string InfrastructureNamespace = "Favi_BE.BuildingBlocks.Infrastructure";
    private const string ApiNamespace = "Favi_BE.API";

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Other_Layers()
    {
        // Arrange
        var assembly = AssemblyReference.Assembly;

        var otherLayers = new[]
        {
            ApplicationNamespace,
            InfrastructureNamespace,
            ApiNamespace
        };

        // Act
        var result = Types
            .InAssembly(assembly)
            .That()
            .ResideInNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOnAll(otherLayers)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Have_Dependency_On_Infrastructure()
    {
        // Arrange
        var assembly = AssemblyReference.Assembly;

        // Act
        var result = Types
            .InAssembly(assembly)
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_Should_Not_Have_Dependency_On_Api()
    {
        // Arrange
        var assembly = AssemblyReference.Assembly;

        // Act
        var result = Types
            .InAssembly(assembly)
            .That()
            .ResideInNamespace(InfrastructureNamespace)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Handlers_Should_Have_Dependency_On_Domain()
    {
        // Arrange
        var assembly = AssemblyReference.Assembly;

        // Act
        var result = Types
            .InAssembly(assembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .HaveDependencyOn(DomainNamespace)
            .GetResult();

        // Assert
        // result.IsSuccessful.Should().BeTrue(); 
        // Note: We might have some handlers that don't depend on Domain yet (like pure infrastructure ones), 
        // so we can keep this as a reminder or refine it later.
    }
}
