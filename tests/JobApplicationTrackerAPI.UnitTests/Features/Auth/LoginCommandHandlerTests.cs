using FluentAssertions;
using FluentValidation;
using JobApplicationTracker.Application.Features.Auth.Commands;
using JobApplicationTracker.Application.Features.Auth.Handlers;
using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Application.Interfaces.Services;
using JobApplicationTracker.Domain.Entities;
using JobApplicationTracker.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;

namespace JobApplicationTrackerAPI.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        _handler = new LoginCommandHandler(
            _mockContext.Object,
            _mockPasswordHasher.Object,
            _mockJwtGenerator.Object);

        _mockJwtGenerator.Setup(x => x.TokenExpirationMinutes).Returns(60);
        _mockJwtGenerator.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("test-token");
        _mockJwtGenerator.Setup(x => x.GenerateRefreshToken()).Returns("test-refresh-token");
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var user = new User("John", "Doe", JobApplicationTracker.Domain.ValueObjects.Email.Create("john@example.com"), "hashed-password");
        SetupUsersAsync(user, true);

        var command = new LoginCommand("john@example.com", "Password123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("test-token");
        result.RefreshToken.Should().Be("test-refresh-token");
    }

    [Fact]
    public async Task Handle_InvalidEmail_ShouldThrowValidationException()
    {
        // Arrange
        SetupUsersAsync(null, false);

        var command = new LoginCommand("nonexistent@example.com", "Password123!");

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_InvalidPassword_ShouldThrowValidationException()
    {
        // Arrange
        var user = new User("John", "Doe", JobApplicationTracker.Domain.ValueObjects.Email.Create("john@example.com"), "hashed-password");
        SetupUsersAsync(user, false);

        var command = new LoginCommand("john@example.com", "WrongPassword");

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Invalid email or password.");
    }

    private void SetupUsersAsync(User? user, bool passwordValid)
    {
        _mockContext.Setup(x => x.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        if (user != null)
        {
            _mockPasswordHasher.Setup(x => x.VerifyPassword(user.PasswordHash, It.IsAny<string>())).Returns(passwordValid);
        }
    }
}
