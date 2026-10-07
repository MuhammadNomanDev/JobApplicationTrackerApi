using AwesomeAssertions;
using JobApplicationTrackerAPI.Api;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Domain.Entities;
using JobApplicationTrackerAPI.Infrastructure.Data;
using NetArchTest.Rules;

namespace JobApplicationTrackerAPI.UnitTests.Architecture;

/// <summary>
/// P1/M1e: the clean-architecture layering is a test, not a hope.
/// Domain → nothing; Application → Domain only; Infrastructure → Domain +
/// Application; nothing references the Api layer except the app host.
/// </summary>
public class ArchitectureTests
{
    private static readonly System.Reflection.Assembly DomainAssembly =
        typeof(JobApplication).Assembly;

    private static readonly System.Reflection.Assembly ApplicationAssembly =
        typeof(ICacheService).Assembly;

    private static readonly System.Reflection.Assembly InfrastructureAssembly =
        typeof(AppDbContext).Assembly;

    private static readonly System.Reflection.Assembly ApiAssembly =
        typeof(Program).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("JobApplicationTrackerAPI.Application")
            .And()
            .NotHaveDependencyOn("JobApplicationTrackerAPI.Infrastructure")
            .And()
            .NotHaveDependencyOn("JobApplicationTrackerAPI.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "the Domain layer must not reference any other layer; got: " +
            string.Join("; ", result.FailingTypes ?? Enumerable.Empty<Type>()));
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn("JobApplicationTrackerAPI.Infrastructure")
            .And()
            .NotHaveDependencyOn("JobApplicationTrackerAPI.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "the Application layer must only depend on Domain; got: " +
            string.Join("; ", result.FailingTypes ?? Enumerable.Empty<Type>()));
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn("JobApplicationTrackerAPI.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Infrastructure must not reference the Api layer; got: " +
            string.Join("; ", result.FailingTypes ?? Enumerable.Empty<Type>()));
    }

    [Fact]
    public void MediatRHandlers_ShouldLiveInApplicationLayer()
    {
        var allAssemblies = new[]
        {
            DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly
        };

        var result = Types.InAssemblies(allAssemblies)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<>))
            .Should()
            .ResideInNamespace("JobApplicationTrackerAPI.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "all MediatR handlers must live in the Application layer; got: " +
            string.Join("; ", result.FailingTypes ?? Enumerable.Empty<Type>()));
    }
}
