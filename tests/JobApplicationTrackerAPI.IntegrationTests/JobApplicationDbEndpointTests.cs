using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using JobApplicationTrackerAPI.Api.Models;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Commands;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Responses;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Domain.Entities;
using JobApplicationTrackerAPI.Domain.Enums;
using JobApplicationTrackerAPI.Domain.ValueObjects;
using JobApplicationTrackerAPI.Infrastructure.Data;
using JobApplicationTrackerAPI.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JobApplicationTrackerAPI.IntegrationTests;

/// <summary>
/// Database-backed endpoint tests (P1/M1e). The three 404 tests were parked
/// with [Skip] since P0 because they need a real database to reach the
/// "not found" path; they now run against a Testcontainers SQL Server.
/// Shares one container per test run via the "SqlServerDatabase" collection.
/// </summary>
[Collection("SqlServerDatabase")]
public class JobApplicationDbEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public JobApplicationDbEndpointTests(
        WebApplicationFactory<Program> factory,
        SqlServerFixture db)
    {
        // Point the app at the containerised SQL Server instead of the
        // (empty) configured connection string.
        _factory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", db.ConnectionString));
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public async Task CreateAndRetrieveJobApplication_RoundTrip()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var command = new CreateJobApplicationCommand(
            "Acme Ltd", "Senior .NET Developer", "https://example.com/jobs/1",
            55000m, JobStatus.Applied, DateTime.UtcNow.Date);

        // Act: create
        var createResponse = await client.PostAsJsonAsync("/api/jobapplications", command);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        created!.Success.Should().BeTrue();

        // Act: retrieve
        var getResponse = await client.GetAsync($"/api/jobapplications/{created.Data}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ApiResponse<JobApplicationDto>>();

        // Assert
        fetched!.Success.Should().BeTrue();
        fetched.Data!.CompanyName.Should().Be("Acme Ltd");
        fetched.Data.PositionTitle.Should().Be("Senior .NET Developer");
        fetched.Data.Status.Should().Be(JobStatus.Applied);
    }

    [Fact]
    public async Task UpdateJobApplication_ExistingId_PersistsChanges()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/jobapplications",
            new CreateJobApplicationCommand("Old Co", "Dev", null, null, JobStatus.Draft, null));
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var update = new UpdateJobApplicationCommand(
            created!.Data, "New Co", "Senior Dev", "https://example.com/jobs/2", 60000m);

        // Act
        var updateResponse = await client.PutAsJsonAsync($"/api/jobapplications/{created.Data}", update);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert
        var getResponse = await client.GetAsync($"/api/jobapplications/{created.Data}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ApiResponse<JobApplicationDto>>();
        fetched!.Data!.CompanyName.Should().Be("New Co");
        fetched.Data.PositionTitle.Should().Be("Senior Dev");
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestToken(new User(
                "Test", "User", Email.Create("test@example.com"), "test-hash")));
        return client;
    }

    /// <summary>
    /// Creates a client whose token belongs to a user that actually exists in
    /// the container database. Required for any test that writes, because
    /// JobApplications.UserId has a foreign key to Users.
    /// </summary>
    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var user = new User(
            "Test", "User",
            Email.Create($"test-{Guid.NewGuid():N}@example.com"),
            "test-hash");
        user.SetRefreshToken(Guid.NewGuid().ToString("N"), DateTime.UtcNow.AddDays(7));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestToken(user));
        return client;
    }

    private string GenerateTestToken(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        return generator.GenerateToken(user);
    }
}
