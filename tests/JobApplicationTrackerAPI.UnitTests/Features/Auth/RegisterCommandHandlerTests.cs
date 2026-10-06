using FluentAssertions;
using FluentValidation;
using JobApplicationTrackerAPI.Application.Features.Auth.Commands;
using JobApplicationTrackerAPI.Application.Features.Auth.Handlers;
using JobApplicationTrackerAPI.Application.Interfaces;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Domain.Entities;
using JobApplicationTrackerAPI.Domain.Interfaces;
using JobApplicationTrackerAPI.UnitTests.Helpers;
using Moq;

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
        SetupUsersAsync(exists: false);

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
        SetupUsersAsync(exists: true);

        var command = new RegisterCommand("John", "Doe", "john@example.com", "Password123!");

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Email already exists.");
    }

    private void SetupUsersAsync(bool exists)
    {
        // NOTE: EF Core's AnyAsync is a static extension method and cannot be
        // mocked with Moq. Mock the DbSet<User> itself instead, so the real
        // extension method executes against the in-memory data.
        var users = exists
            ? new[] { new User("John", "Doe", JobApplicationTrackerAPI.Domain.ValueObjects.Email.Create("john@example.com"), "hashed-password") }
            : Enumerable.Empty<User>();
        _mockContext.Setup(x => x.Users).Returns(MockDbSetHelper.Create(users).Object);
    }
}
