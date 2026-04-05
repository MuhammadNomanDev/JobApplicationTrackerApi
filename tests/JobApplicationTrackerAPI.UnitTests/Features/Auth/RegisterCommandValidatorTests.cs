using FluentValidation.TestHelper;
using JobApplicationTracker.Application.Features.Auth.Commands;

namespace JobApplicationTrackerAPI.UnitTests.Features.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_FirstName_Is_Empty()
    {
        var command = new RegisterCommand("", "Doe", "john@example.com", "Password123!");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Invalid()
    {
        var command = new RegisterCommand("John", "Doe", "invalid-email", "Password123!");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Too_Short()
    {
        var command = new RegisterCommand("John", "Doe", "john@example.com", "Short1!");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new RegisterCommand("John", "Doe", "john@example.com", "Password123!");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
