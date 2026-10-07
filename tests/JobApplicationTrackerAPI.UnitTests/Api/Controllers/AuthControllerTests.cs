using AwesomeAssertions;
using JobApplicationTrackerAPI.Api.Controllers;
using JobApplicationTrackerAPI.Api.Models;
using JobApplicationTrackerAPI.Application.Features.Auth.Commands;
using JobApplicationTrackerAPI.Application.Features.Auth.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Api.Controllers;

/// <summary>
/// P1/M1j: auth controller tests.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _sut = new AuthController(_mockMediator.Object);
    }

    private static AuthResponse Response()
        => new("jwt", "refresh", DateTime.UtcNow.AddMinutes(60));

    [Fact]
    public async Task Register_SendsCommand_AndReturnsOk()
    {
        var command = new RegisterCommand("Test", "User", "t@example.com", "password123");
        var response = Response();
        _mockMediator
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.Register(command);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<ApiResponse<AuthResponse>>()
            .Which.Should().Match<ApiResponse<AuthResponse>>(r =>
                r.Success && r.Data == response && r.Message == "User registered successfully.");
    }

    [Fact]
    public async Task Login_SendsCommand_AndReturnsOk()
    {
        var command = new LoginCommand("t@example.com", "password123");
        var response = Response();
        _mockMediator
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.Login(command);

        _mockMediator.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<ApiResponse<AuthResponse>>()
            .Which.Data.Should().BeSameAs(response);
    }

    [Fact]
    public async Task RefreshToken_SendsCommand_AndReturnsOk()
    {
        var command = new RefreshTokenCommand("old-jwt", "old-refresh");
        var response = Response();
        _mockMediator
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.RefreshToken(command);

        _mockMediator.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }
}
