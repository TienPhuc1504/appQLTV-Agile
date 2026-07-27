using FluentAssertions;
using LibraryManagement.Core;
using LibraryManagement.Infrastructure;

namespace LibraryManagement.Tests.Unit;

public sealed class ArchitectureSmokeTests
{
    [Fact]
    public void CoreProject_ShouldNotReferenceInfrastructureOrApp()
    {
        string[] referencedAssemblies = typeof(CoreAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .ToArray();

        referencedAssemblies.Should().NotContain("LibraryManagement.Infrastructure");
        referencedAssemblies.Should().NotContain("LibraryManagement.App");
    }

    [Fact]
    public void InfrastructureProject_ShouldNotReferenceApp()
    {
        string[] referencedAssemblies = typeof(DependencyInjection)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .ToArray();

        referencedAssemblies.Should().NotContain("LibraryManagement.App");
    }
}
