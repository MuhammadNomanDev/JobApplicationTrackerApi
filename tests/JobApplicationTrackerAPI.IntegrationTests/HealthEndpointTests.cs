using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace JobApplicationTrackerAPI.IntegrationTests;

/// <summary>
/// P1/M1c split the single /health endpoint into liveness and readiness probes.
/// </summary>
public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Live_ReturnsOk_WithoutRunningDependencyChecks()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert: the process is up; no dependency is probed here.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_WithoutConfiguredDatabase_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert: no connection string is configured in the test environment,
        // so no dependency check is registered and the app is trivially ready.
        // (Registering the SQL Server check with an empty connection string
        // would throw in its DI factory — outside the check's own try/catch —
        // and escape as a 500, so registration is conditional.)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_WithUnreachableDatabase_ReturnsServiceUnavailable()
    {
        // Arrange: point the app at a database that refuses connections.
        // (UseSetting lands in configuration with high precedence, so the
        // app registers the SQL Server check against this connection string.)
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Server=127.0.0.1,1433;Database=master;User Id=sa;Password=not-the-password;TrustServerCertificate=True;Connect Timeout=2"));
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert: the dependency is configured but unreachable, so the
        // readiness gate reports 503 — the pod leaves load-balancer rotation
        // instead of serving traffic it cannot fulfil.
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task LegacyHealthEndpoint_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert: the old single endpoint was removed in P1/M1c.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
