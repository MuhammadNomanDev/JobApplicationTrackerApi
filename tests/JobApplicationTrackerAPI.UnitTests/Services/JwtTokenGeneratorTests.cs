using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AwesomeAssertions;
using JobApplicationTrackerAPI.Application.Settings;
using JobApplicationTrackerAPI.Domain.Entities;
using JobApplicationTrackerAPI.Domain.ValueObjects;
using JobApplicationTrackerAPI.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace JobApplicationTrackerAPI.UnitTests.Services;

/// <summary>
/// P1/M1i: JWT generator tests. Tokens are parsed (not validated) to assert
/// the claims this API depends on — see CreateJobApplicationCommandHandler,
/// which reads ClaimTypes.NameIdentifier via IHttpContextAccessor.
/// </summary>
public class JwtTokenGeneratorTests
{
    private const string Key = "M1i-Test-Only-Signing-Key-0123456789abcdef";

    private static JwtTokenGenerator CreateGenerator(int expirationMinutes = 60)
    {
        var settings = new JwtSettings
        {
            Key = Key,
            Issuer = "JobApplicationTracker.Tests",
            Audience = "JobApplicationTracker.Tests",
            TokenExpirationMinutes = expirationMinutes
        };
        return new JwtTokenGenerator(Options.Create(settings));
    }

    private static User TestUser()
        => new("Test", "User", Email.Create("test.user@example.com"), "hash");

    [Fact]
    public void GenerateToken_ContainsExpectedClaims()
    {
        var user = TestUser();
        var generator = CreateGenerator();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(generator.GenerateToken(user));

        jwt.Claims.Should().ContainSingle(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().ContainSingle(c =>
            c.Type == ClaimTypes.Email && c.Value == "test.user@example.com");
        jwt.Claims.Should().ContainSingle(c =>
            c.Type == ClaimTypes.GivenName && c.Value == "Test");
        jwt.Claims.Should().ContainSingle(c =>
            c.Type == ClaimTypes.Surname && c.Value == "User");
        jwt.Issuer.Should().Be("JobApplicationTracker.Tests");
        jwt.Audiences.Should().Contain("JobApplicationTracker.Tests");
    }

    [Fact]
    public void GenerateToken_ExpiresAtConfiguredMinutes()
    {
        var generator = CreateGenerator(expirationMinutes: 30);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(generator.GenerateToken(TestUser()));

        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        var generator = CreateGenerator();

        var first = generator.GenerateRefreshToken();
        var second = generator.GenerateRefreshToken();

        first.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(first).Should().HaveCount(32);
        second.Should().NotBe(first);
    }

    [Fact]
    public void TokenExpirationMinutes_ReturnsConfiguredValue()
    {
        CreateGenerator(expirationMinutes: 15).TokenExpirationMinutes.Should().Be(15);
    }
}
