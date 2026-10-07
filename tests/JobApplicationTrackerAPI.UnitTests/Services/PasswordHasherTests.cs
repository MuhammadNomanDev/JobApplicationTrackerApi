using AwesomeAssertions;
using JobApplicationTrackerAPI.Infrastructure.Services;

namespace JobApplicationTrackerAPI.UnitTests.Services;

/// <summary>
/// P1/M1i: password hasher round-trip tests (BCrypt, work factor 12).
/// </summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_ThenVerify_ReturnsTrue()
    {
        var hash = _sut.HashPassword("correct-horse-123");

        _sut.VerifyPassword(hash, "correct-horse-123").Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword("correct-horse-123");

        _sut.VerifyPassword(hash, "wrong-password").Should().BeFalse();
    }

    [Fact]
    public void HashPassword_Twice_ProducesDifferentHashes()
    {
        var first = _sut.HashPassword("same-password");
        var second = _sut.HashPassword("same-password");

        first.Should().NotBe(second);
        _sut.VerifyPassword(first, "same-password").Should().BeTrue();
        _sut.VerifyPassword(second, "same-password").Should().BeTrue();
    }
}
