using AwesomeAssertions;
using JobApplicationTrackerAPI.Api.Controllers;
using JobApplicationTrackerAPI.Api.Models;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Commands;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Queries;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Responses;
using JobApplicationTrackerAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Api.Controllers;

/// <summary>
/// P1/M1j: controller tests with a mocked IMediator. Controllers are thin
/// adapters: assert the right command/query is sent and the right HTTP
/// result comes back.
/// </summary>
public class JobApplicationsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly JobApplicationsController _sut;

    public JobApplicationsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _sut = new JobApplicationsController(_mockMediator.Object);
    }

    private static JobApplicationDto Dto(string company = "Acme")
        => new(Guid.NewGuid(), company, "Dev", null, null, JobStatus.Draft,
            null, DateTime.UtcNow, null);

    [Fact]
    public async Task GetAll_ParsesStatusFilter_AndReturnsOk()
    {
        var page = new PagedResult<JobApplicationDto>([Dto()], 1, 10, 1);
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetAllJobApplicationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _sut.GetAllJobApplications(1, 10, "Applied", "acme");

        _mockMediator.Verify(m => m.Send(
            It.Is<GetAllJobApplicationsQuery>(q =>
                q.Page == 1 && q.PageSize == 10 &&
                q.Status == JobStatus.Applied && q.SearchTerm == "acme"),
            It.IsAny<CancellationToken>()), Times.Once);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<ApiResponse<PagedResult<JobApplicationDto>>>()
            .Which.Data.Should().BeSameAs(page);
    }

    [Fact]
    public async Task GetAll_InvalidStatus_SendsNullFilter()
    {
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetAllJobApplicationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<JobApplicationDto>([], 1, 10, 0));

        await _sut.GetAllJobApplications(status: "not-a-status");

        _mockMediator.Verify(m => m.Send(
            It.Is<GetAllJobApplicationsQuery>(q => q.Status == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_ReturnsOkWithDto()
    {
        var dto = Dto();
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetJobApplicationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _sut.GetJobApplication(dto.Id);

        _mockMediator.Verify(m => m.Send(
            It.Is<GetJobApplicationQuery>(q => q.Id == dto.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<ApiResponse<JobApplicationDto>>()
            .Which.Data.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Create_Returns201WithLocation()
    {
        var id = Guid.NewGuid();
        var command = new CreateJobApplicationCommand("Acme", "Dev", null, null, JobStatus.Draft, null);
        _mockMediator
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(id);

        var result = await _sut.CreateJobApplication(command);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.ActionName.Should().Be(nameof(JobApplicationsController.GetJobApplication));
        created.RouteValues!["id"].Should().Be(id);
    }

    [Fact]
    public async Task Update_IdMismatch_ReturnsBadRequest()
    {
        var command = new UpdateJobApplicationCommand(Guid.NewGuid(), "A", "B", null, null);

        var result = await _sut.UpdateJobApplication(Guid.NewGuid(), command);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mockMediator.Verify(m => m.Send(
            It.IsAny<UpdateJobApplicationCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_IdMatch_SendsCommand_AndReturns204()
    {
        var command = new UpdateJobApplicationCommand(Guid.NewGuid(), "A", "B", null, null);
        _mockMediator
            .Setup(m => m.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdateJobApplication(command.Id, command);

        _mockMediator.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateStatus_IdMismatch_ReturnsBadRequest()
    {
        var command = new UpdateJobApplicationStatusCommand(Guid.NewGuid(), JobStatus.Applied);

        var result = await _sut.UpdateJobApplicationStatus(Guid.NewGuid(), command);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_IdMatch_Returns204()
    {
        var command = new UpdateJobApplicationStatusCommand(Guid.NewGuid(), JobStatus.Applied);
        _mockMediator
            .Setup(m => m.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdateJobApplicationStatus(command.Id, command);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_SendsCommand_AndReturns204()
    {
        var id = Guid.NewGuid();
        _mockMediator
            .Setup(m => m.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteJobApplication(id);

        _mockMediator.Verify(m => m.Send(
            It.Is<DeleteJobApplicationCommand>(c => c.Id == id),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<NoContentResult>();
    }
}
