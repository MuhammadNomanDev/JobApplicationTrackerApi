using AwesomeAssertions;
using JobApplicationTrackerAPI.Application.Features.Notes.Commands;
using JobApplicationTrackerAPI.Application.Features.Notes.Handlers;
using JobApplicationTrackerAPI.Application.Features.Notes.Queries;
using JobApplicationTrackerAPI.Application.Interfaces;
using JobApplicationTrackerAPI.Domain.Entities;
using JobApplicationTrackerAPI.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Features.Notes;

/// <summary>
/// P1/M1h: notes handler unit tests.
/// </summary>
public class NoteHandlersTests
{
    private readonly Mock<IAppDbContext> _mockContext;

    public NoteHandlersTests()
    {
        _mockContext = new Mock<IAppDbContext>();
    }

    private void Seed(JobApplication[] apps, Note[] notes)
    {
        _mockContext.Setup(x => x.JobApplications)
            .Returns(MockDbSetHelper.Create(apps).Object);
        _mockContext.Setup(x => x.Notes)
            .Returns(MockDbSetHelper.Create(notes).Object);
    }

    [Fact]
    public async Task CreateNote_ExistingJobApplication_ReturnsNoteId()
    {
        var app = new JobApplication("Acme", "Dev", Guid.NewGuid());
        Seed(new[] { app }, Array.Empty<Note>());
        var handler = new CreateNoteCommandHandler(_mockContext.Object);

        var id = await handler.Handle(
            new CreateNoteCommand(app.Id, "Call them Tuesday"), CancellationToken.None);

        id.Should().NotBeEmpty();
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateNote_MissingJobApplication_ThrowsKeyNotFound()
    {
        Seed(Array.Empty<JobApplication>(), Array.Empty<Note>());
        var handler = new CreateNoteCommandHandler(_mockContext.Object);

        var act = () => handler.Handle(
            new CreateNoteCommand(Guid.NewGuid(), "x"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateNote_Existing_UpdatesContent()
    {
        var note = new Note("old", Guid.NewGuid());
        Seed(Array.Empty<JobApplication>(), new[] { note });
        var handler = new UpdateNoteCommandHandler(_mockContext.Object);

        await handler.Handle(new UpdateNoteCommand(note.Id, "new"), CancellationToken.None);

        note.Content.Should().Be("new");
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateNote_Missing_ThrowsKeyNotFound()
    {
        Seed(Array.Empty<JobApplication>(), Array.Empty<Note>());
        var handler = new UpdateNoteCommandHandler(_mockContext.Object);

        var act = () => handler.Handle(
            new UpdateNoteCommand(Guid.NewGuid(), "x"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteNote_Existing_RemovesNote()
    {
        var note = new Note("bye", Guid.NewGuid());
        var mockSet = MockDbSetHelper.Create(new[] { note });
        _mockContext.Setup(x => x.Notes).Returns(mockSet.Object);
        var handler = new DeleteNoteCommandHandler(_mockContext.Object);

        await handler.Handle(new DeleteNoteCommand(note.Id), CancellationToken.None);

        mockSet.Verify(x => x.Remove(note), Times.Once);
    }

    [Fact]
    public async Task DeleteNote_Missing_ThrowsKeyNotFound()
    {
        Seed(Array.Empty<JobApplication>(), Array.Empty<Note>());
        var handler = new DeleteNoteCommandHandler(_mockContext.Object);

        var act = () => handler.Handle(
            new DeleteNoteCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetNotesByJobApplication_ReturnsNewestFirst()
    {
        var jobAppId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        Seed(Array.Empty<JobApplication>(), new[]
        {
            new Note("first", jobAppId),
            new Note("other app", otherId),
            new Note("second", jobAppId),
        });
        var handler = new GetNotesByJobApplicationQueryHandler(_mockContext.Object);

        var result = (await handler.Handle(
            new GetNotesByJobApplicationQuery(jobAppId), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(n => n.JobApplicationId == jobAppId);
    }
}
