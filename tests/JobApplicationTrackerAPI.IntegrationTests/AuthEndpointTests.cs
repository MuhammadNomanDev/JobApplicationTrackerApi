using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using JobApplicationTracker.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using JobApplicationTracker.Application.Interfaces.Services;

namespace JobApplicationTrackerAPI.IntegrationTests;

public class AuthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_InvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new RegisterCommand("", "", "invalid-email", "short");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_InvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new LoginCommand("", "");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_EmptyToken_ShouldReturnBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new RefreshTokenCommand("", "");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetJobApplications_Unauthorized_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/jobapplications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
