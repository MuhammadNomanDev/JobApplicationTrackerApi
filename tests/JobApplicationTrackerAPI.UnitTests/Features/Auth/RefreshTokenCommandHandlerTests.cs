using AwesomeAssertions;
using FluentValidation;
using JobApplicationTrackerAPI.Application.Features.Auth.Commands;
using JobApplicationTrackerAPI.Application.Features.Auth.Handlers;
using JobApplicationTrackerAPI.Application.Interfaces;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Domain.Entities;
using JobApplicationTrackerAPI.Domain.Interfaces;
using JobApplicationTrackerAPI.Domain.ValueObjects;
using JobApplicationTrackerAPI.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Features.Auth;

/// <summary>
/// P1/M1h: refresh-token handler tests. The login/register handlers were
/// already covered; this one was not.
/// </summary>
public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IJwtTokenGenerator> _mockJwt;
    private readonly Mock<IPasswordHasher> _mockHasher;

    public RefreshTokenCommandHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockJwt = new Mock<IJwtTokenGenerator>();
        _mockHasher = new Mock<IPasswordHasher>();
        _mockJwt.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("new-jwt");
        _mockJwt.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh");
        _mockJwt.Setup(x => x.TokenExpirationMinutes).Returns(60);
    }

    private static User UserWithRefreshToken(DateTime expiry)
    {
        var user = new User("Test", "User", Email.Create("t@example.com"), "hash");
        user.SetRefreshToken("valid-refresh", expiry);
        return user;
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewTokensAndRotates()
    {
        var user = UserWithRefreshToken(DateTime.UtcNow.AddDays(1));
        _mockContext.Setup(x => x.Users)
            .Returns(MockDbSetHelper.Create(new[] { user }).Object);
        var handler = new RefreshTokenCommandHandler(_mockContext.Object, _mockHasher.Object, _mockJwt.Object);

        var response = await handler.Handle(
            new RefreshTokenCommand("old-jwt", "valid-refresh"), CancellationToken.None);

        response.Token.Should().Be("new-jwt");
        response.RefreshToken.Should().Be("new-refresh");
        user.RefreshToken.Should().Be("new-refresh");
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownToken_ThrowsValidationException()
    {
        _mockContext.Setup(x => x.Users)
            .Returns(MockDbSetHelper.Create(Array.Empty<User>()).Object);
        var handler = new RefreshTokenCommandHandler(_mockContext.Object, _mockHasher.Object, _mockJwt.Object);

        var act = () => handler.Handle(
            new RefreshTokenCommand("old-jwt", "nope"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid refresh token*");
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsValidationException()
    {
        var user = UserWithRefreshToken(DateTime.UtcNow.AddDays(-1));
        _mockContext.Setup(x => x.Users)
            .Returns(MockDbSetHelper.Create(new[] { user }).Object);
        var handler = new RefreshTokenCommandHandler(_mockContext.Object, _mockHasher.Object, _mockJwt.Object);

        var act = () => handler.Handle(
            new RefreshTokenCommand("old-jwt", "valid-refresh"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*expired*");
    }
}
