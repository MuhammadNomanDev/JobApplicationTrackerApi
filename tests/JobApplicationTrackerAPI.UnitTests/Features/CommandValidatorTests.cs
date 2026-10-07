using AwesomeAssertions;
using JobApplicationTrackerAPI.Application.Features.Auth.Commands;
using JobApplicationTrackerAPI.Application.Features.Documents.Commands;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Commands;
using JobApplicationTrackerAPI.Application.Features.Notes.Commands;
using JobApplicationTrackerAPI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Features;

/// <summary>
/// P1/M1g: every command validator gets direct tests. Validators are pure
/// functions of their input — no mocks, no database, just rules.
/// </summary>
public class CommandValidatorTests
{
    private static Mock<IFormFile> ValidFile(
        long length = 1024, string contentType = "application/pdf")
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(length);
        file.Setup(f => f.ContentType).Returns(contentType);
        return file;
    }

    [Fact]
    public void CreateDocumentValidator_ValidFile_Passes()
    {
        var validator = new CreateDocumentCommandValidator();
        var command = new CreateDocumentCommand(
            Guid.NewGuid(), ValidFile().Object, DocumentType.CV);

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateDocumentValidator_NullFile_Fails()
    {
        var validator = new CreateDocumentCommandValidator();
        var command = new CreateDocumentCommand(
            Guid.NewGuid(), null!, DocumentType.CV);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("File is required"));
    }

    [Fact]
    public void CreateDocumentValidator_OversizeFile_Fails()
    {
        var validator = new CreateDocumentCommandValidator();
        var command = new CreateDocumentCommand(
            Guid.NewGuid(), ValidFile(length: 11 * 1024 * 1024).Object, DocumentType.CV);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("must not exceed 10MB"));
    }

    [Fact]
    public void CreateDocumentValidator_DisallowedContentType_Fails()
    {
        var validator = new CreateDocumentCommandValidator();
        var command = new CreateDocumentCommand(
            Guid.NewGuid(), ValidFile(contentType: "application/zip").Object, DocumentType.CV);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("not allowed"));
    }

    [Fact]
    public void CreateDocumentValidator_EmptyJobApplicationId_Fails()
    {
        var validator = new CreateDocumentCommandValidator();
        var command = new CreateDocumentCommand(
            Guid.Empty, ValidFile().Object, DocumentType.CV);

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateDocumentValidator_InvalidDocumentType_Fails()
    {
        var validator = new CreateDocumentCommandValidator();
        var command = new CreateDocumentCommand(
            Guid.NewGuid(), ValidFile().Object, (DocumentType)999);

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateJobApplicationValidator_Valid_Passes()
    {
        var validator = new UpdateJobApplicationCommandValidator();
        var command = new UpdateJobApplicationCommand(
            Guid.NewGuid(), "Acme", "Dev", "https://example.com", 50000m);

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateJobApplicationValidator_EmptyCompany_Fails()
    {
        var validator = new UpdateJobApplicationCommandValidator();
        var command = new UpdateJobApplicationCommand(
            Guid.NewGuid(), "", "Dev", null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Company name is required"));
    }

    [Fact]
    public void UpdateJobApplicationValidator_TooLongCompany_Fails()
    {
        var validator = new UpdateJobApplicationCommandValidator();
        var command = new UpdateJobApplicationCommand(
            Guid.NewGuid(), new string('x', 201), "Dev", null, null);

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateJobApplicationValidator_NegativeSalary_Fails()
    {
        var validator = new UpdateJobApplicationCommandValidator();
        var command = new UpdateJobApplicationCommand(
            Guid.NewGuid(), "Acme", "Dev", null, -1m);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("positive value"));
    }

    [Fact]
    public void UpdateJobApplicationStatusValidator_Valid_Passes()
    {
        var validator = new UpdateJobApplicationStatusCommandValidator();
        var command = new UpdateJobApplicationStatusCommand(Guid.NewGuid(), JobStatus.Interviewing);

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateJobApplicationStatusValidator_EmptyId_Fails()
    {
        var validator = new UpdateJobApplicationStatusCommandValidator();
        var command = new UpdateJobApplicationStatusCommand(Guid.Empty, JobStatus.Applied);

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateJobApplicationStatusValidator_InvalidStatus_Fails()
    {
        var validator = new UpdateJobApplicationStatusCommandValidator();
        var command = new UpdateJobApplicationStatusCommand(Guid.NewGuid(), (JobStatus)999);

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateNoteValidator_Valid_Passes()
    {
        var validator = new CreateNoteCommandValidator();
        var command = new CreateNoteCommand(Guid.NewGuid(), "Remember to follow up");

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateNoteValidator_EmptyContent_Fails()
    {
        var validator = new CreateNoteCommandValidator();
        var command = new CreateNoteCommand(Guid.NewGuid(), "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Note content is required"));
    }

    [Fact]
    public void CreateNoteValidator_TooLongContent_Fails()
    {
        var validator = new CreateNoteCommandValidator();
        var command = new CreateNoteCommand(Guid.NewGuid(), new string('x', 2001));

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateNoteValidator_Valid_Passes()
    {
        var validator = new UpdateNoteCommandValidator();
        var command = new UpdateNoteCommand(Guid.NewGuid(), "Updated");

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateNoteValidator_EmptyId_Fails()
    {
        var validator = new UpdateNoteCommandValidator();
        var command = new UpdateNoteCommand(Guid.Empty, "Updated");

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateJobApplicationValidator_NegativeSalary_Fails()
    {
        var validator = new CreateJobApplicationCommandValidator();
        var command = new CreateJobApplicationCommand(
            "Acme", "Dev", null, -100m, JobStatus.Draft, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("positive value"));
    }

    [Fact]
    public void CreateJobApplicationValidator_TooLongUrl_Fails()
    {
        var validator = new CreateJobApplicationCommandValidator();
        var command = new CreateJobApplicationCommand(
            "Acme", "Dev", "https://x/" + new string('y', 500), null, JobStatus.Draft, null);

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void LoginValidator_InvalidEmail_Fails()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("not-an-email", "Password1!");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Invalid email format"));
    }

    [Fact]
    public void LoginValidator_EmptyPassword_Fails()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("test@example.com", "");

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RefreshTokenValidator_EmptyTokens_Fails()
    {
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand("", "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}
