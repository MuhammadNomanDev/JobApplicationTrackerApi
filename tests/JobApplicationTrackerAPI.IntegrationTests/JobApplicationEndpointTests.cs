using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Commands;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Domain.Entities;
using JobApplicationTrackerAPI.Domain.Enums;
using JobApplicationTrackerAPI.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JobApplicationTrackerAPI.IntegrationTests;

public class JobApplicationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public JobApplicationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateJobApplication_InvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var command = new CreateJobApplicationCommand("", "", null, null, JobStatus.Draft, null);

        // Act
        var response = await client.PostAsJsonAsync("/api/jobapplications", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires a real database; will be re-enabled with Testcontainers SQL Server in P1/M1")]
    public async Task GetJobApplication_NonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/jobapplications/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Skip = "Requires a real database; will be re-enabled with Testcontainers SQL Server in P1/M1")]
    public async Task UpdateJobApplication_NonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var nonExistentId = Guid.NewGuid();
        var command = new UpdateJobApplicationCommand(nonExistentId, "Company", "Position", null, null);

        // Act
        var response = await client.PutAsJsonAsync($"/api/jobapplications/{nonExistentId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Skip = "Requires a real database; will be re-enabled with Testcontainers SQL Server in P1/M1")]
    public async Task DeleteJobApplication_NonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/jobapplications/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestToken());
        return client;
    }

    private string GenerateTestToken()
    {
        // Use the app's own token generator so the token validates against the
        // same Jwt settings the API was configured with (provided via env vars in CI).
        using var scope = _factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var user = new User("Test", "User", Email.Create("test@example.com"), "test-hash");
        return generator.GenerateToken(user);
    }
}
