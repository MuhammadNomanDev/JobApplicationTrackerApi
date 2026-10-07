using AwesomeAssertions;
using JobApplicationTrackerAPI.Domain.Entities;
using JobApplicationTrackerAPI.Domain.Enums;
using JobApplicationTrackerAPI.Domain.ValueObjects;

namespace JobApplicationTrackerAPI.UnitTests.Domain;

/// <summary>
/// P1/M1g: the domain entities are the cheapest lines to cover and the most
/// embarrassing to leave uncovered — pure logic, no mocking needed.
/// </summary>
public class EntityTests
{
    [Fact]
    public void Document_Constructor_SetsProperties()
    {
        var jobAppId = Guid.NewGuid();

        var doc = new Document("cv.pdf", "https://blobs/cv.pdf", DocumentType.CV, jobAppId);

        doc.FileName.Should().Be("cv.pdf");
        doc.FileUrl.Should().Be("https://blobs/cv.pdf");
        doc.DocumentType.Should().Be(DocumentType.CV);
        doc.JobApplicationId.Should().Be(jobAppId);
        doc.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Document_UpdateDocument_UpdatesFieldsAndTimestamp()
    {
        var doc = new Document("a.pdf", "https://x/a.pdf", DocumentType.CV, Guid.NewGuid());

        doc.UpdateDocument("b.pdf", "https://x/b.pdf");

        doc.FileName.Should().Be("b.pdf");
        doc.FileUrl.Should().Be("https://x/b.pdf");
        doc.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Note_Constructor_SetsProperties()
    {
        var jobAppId = Guid.NewGuid();

        var note = new Note("Follow up on Friday", jobAppId);

        note.Content.Should().Be("Follow up on Friday");
        note.JobApplicationId.Should().Be(jobAppId);
        note.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Note_UpdateContent_UpdatesContentAndTimestamp()
    {
        var note = new Note("old", Guid.NewGuid());

        note.UpdateContent("new");

        note.Content.Should().Be("new");
        note.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void JobApplication_Constructor_StartsAsDraft()
    {
        var userId = Guid.NewGuid();

        var app = new JobApplication("Acme", "Dev", userId);

        app.CompanyName.Should().Be("Acme");
        app.PositionTitle.Should().Be("Dev");
        app.UserId.Should().Be(userId);
        app.Status.Should().Be(JobStatus.Draft);
        app.AppliedDate.Should().BeNull();
    }

    [Fact]
    public void JobApplication_UpdateDetails_UpdatesAllFields()
    {
        var app = new JobApplication("Old", "Junior", Guid.NewGuid());

        app.UpdateDetails("New", "Senior", "https://example.com/j", 60000m);

        app.CompanyName.Should().Be("New");
        app.PositionTitle.Should().Be("Senior");
        app.JobUrl.Should().Be("https://example.com/j");
        app.Salary.Should().Be(60000m);
        app.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void JobApplication_UpdateStatus_ToApplied_SetsAppliedDate()
    {
        var app = new JobApplication("Acme", "Dev", Guid.NewGuid());

        app.UpdateStatus(JobStatus.Applied);

        app.Status.Should().Be(JobStatus.Applied);
        app.AppliedDate.Should().NotBeNull();
    }

    [Fact]
    public void JobApplication_UpdateStatus_ToApplied_KeepsExistingAppliedDate()
    {
        var app = new JobApplication("Acme", "Dev", Guid.NewGuid());
        app.UpdateStatus(JobStatus.Applied);
        var firstDate = app.AppliedDate;

        app.UpdateStatus(JobStatus.Interviewing);
        app.UpdateStatus(JobStatus.Applied);

        app.AppliedDate.Should().Be(firstDate);
    }

    [Fact]
    public void JobApplication_UpdateStatus_NonApplied_DoesNotSetAppliedDate()
    {
        var app = new JobApplication("Acme", "Dev", Guid.NewGuid());

        app.UpdateStatus(JobStatus.Interviewing);

        app.Status.Should().Be(JobStatus.Interviewing);
        app.AppliedDate.Should().BeNull();
    }

    [Fact]
    public void User_Constructor_SetsProperties()
    {
        var email = Email.Create("noman@example.com");

        var user = new User("Muhammad", "Noman", email, "hash");

        user.FirstName.Should().Be("Muhammad");
        user.LastName.Should().Be("Noman");
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be("hash");
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void User_UpdateName_UpdatesNames()
    {
        var user = new User("A", "B", Email.Create("a@b.com"), "hash");

        user.UpdateName("C", "D");

        user.FirstName.Should().Be("C");
        user.LastName.Should().Be("D");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void User_UpdatePassword_UpdatesHash()
    {
        var user = new User("A", "B", Email.Create("a@b.com"), "old");

        user.UpdatePassword("new");

        user.PasswordHash.Should().Be("new");
    }

    [Fact]
    public void User_SetRefreshToken_SetsTokenAndExpiry()
    {
        var user = new User("A", "B", Email.Create("a@b.com"), "hash");
        var expiry = DateTime.UtcNow.AddDays(7);

        user.SetRefreshToken("tok123", expiry);

        user.RefreshToken.Should().Be("tok123");
        user.RefreshTokenExpiryTime.Should().Be(expiry);
    }

    [Fact]
    public void Email_Create_Valid_ReturnsEmail()
    {
        var email = Email.Create("test@example.com");

        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Email_Create_Empty_ThrowsArgumentException()
    {
        var act = () => Email.Create("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Email_Create_WithoutAtSign_ThrowsArgumentException()
    {
        var act = () => Email.Create("not-an-email");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Email_ImplicitConversion_ReturnsValue()
    {
        Email email = Email.Create("test@example.com");

        string value = email;

        value.Should().Be("test@example.com");
    }
}
