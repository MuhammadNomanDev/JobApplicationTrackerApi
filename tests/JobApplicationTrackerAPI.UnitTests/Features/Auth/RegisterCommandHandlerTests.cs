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

public class RegisterCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        _handler = new RegisterCommandHandler(
            _mockContext.Object,
            _mockPasswordHasher.Object,
            _mockJwtGenerator.Object);

        _mockJwtGenerator.Setup(x => x.TokenExpirationMinutes).Returns(60);
        _mockJwtGenerator.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("test-token");
        _mockJwtGenerator.Setup(x => x.GenerateRefreshToken()).Returns("test-refresh-token");
        _mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnAuthResponse()
    {
        // Arrange
        var command = new RegisterCommand("John", "Doe", "john@example.com", "Password123!");
        SetupUsersAsync(false, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("test-token");
        result.RefreshToken.Should().Be("test-refresh-token");
        _mockContext.Verify(x => x.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ShouldThrowValidationException()
    {
        // Arrange
        SetupUsersAsync(true, null);

        var command = new RegisterCommand("John", "Doe", "john@example.com", "Password123!");

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Email already exists.");
    }

    private void SetupUsersAsync(bool exists, User? user)
    {
        _mockContext.Setup(x => x.Users.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);
        _mockContext.Setup(x => x.Users.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }
}
