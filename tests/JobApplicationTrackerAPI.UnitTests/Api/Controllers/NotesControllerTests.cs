using AwesomeAssertions;
using JobApplicationTrackerAPI.Api.Controllers;
using JobApplicationTrackerAPI.Api.Models;
using JobApplicationTrackerAPI.Application.Features.Notes.Commands;
using JobApplicationTrackerAPI.Application.Features.Notes.Queries;
using JobApplicationTrackerAPI.Application.Features.Notes.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Api.Controllers;

/// <summary>
/// P1/M1j: notes controller tests.
/// </summary>
public class NotesControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly NotesController _sut;

    public NotesControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _sut = new NotesController(_mockMediator.Object);
    }

    [Fact]
    public async Task GetNotesByJobApplication_ReturnsOk()
    {
        var jobAppId = Guid.NewGuid();
        var notes = new List<NoteDto>
        {
            new(Guid.NewGuid(), "hello", jobAppId, DateTime.UtcNow, null)
        };
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetNotesByJobApplicationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        var result = await _sut.GetNotesByJobApplication(jobAppId);

        _mockMediator.Verify(m => m.Send(
            It.Is<GetNotesByJobApplicationQuery>(q => q.JobApplicationId == jobAppId),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateNote_Returns201()
    {
        var command = new CreateNoteCommand(Guid.NewGuid(), "Call Tuesday");
        var id = Guid.NewGuid();
        _mockMediator
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(id);

        var result = await _sut.CreateNote(command);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.ActionName.Should().Be(nameof(NotesController.GetNotesByJobApplication));
        created.RouteValues!["jobApplicationId"].Should().Be(command.JobApplicationId);
    }

    [Fact]
    public async Task UpdateNote_IdMismatch_ReturnsBadRequest()
    {
        var command = new UpdateNoteCommand(Guid.NewGuid(), "x");

        var result = await _sut.UpdateNote(Guid.NewGuid(), command);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mockMediator.Verify(m => m.Send(
            It.IsAny<UpdateNoteCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateNote_IdMatch_Returns204()
    {
        var command = new UpdateNoteCommand(Guid.NewGuid(), "x");
        _mockMediator
            .Setup(m => m.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdateNote(command.Id, command);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteNote_Returns204()
    {
        var id = Guid.NewGuid();
        _mockMediator
            .Setup(m => m.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteNote(id);

        _mockMediator.Verify(m => m.Send(
            It.Is<DeleteNoteCommand>(c => c.Id == id),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<NoContentResult>();
    }
}
