using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using JobApplicationTracker.Application.Features.JobApplications.Commands;
using JobApplicationTracker.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

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
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "fake-token");
        var command = new CreateJobApplicationCommand("", "", null, null, JobStatus.Draft, null);

        // Act
        var response = await client.PostAsJsonAsync("/api/jobapplications", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetJobApplication_NonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "fake-token");
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
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "fake-token");
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
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "fake-token");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/jobapplications/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
