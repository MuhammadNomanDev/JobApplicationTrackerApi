using AwesomeAssertions;
using JobApplicationTrackerAPI.Application.Events;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Commands;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Handlers;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Queries;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Responses;
using JobApplicationTrackerAPI.Application.Interfaces;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Domain.Entities;
using JobApplicationTrackerAPI.Domain.Enums;
using JobApplicationTrackerAPI.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Features.JobApplications;

/// <summary>
/// P1/M1h: handler unit tests with mocked DbSets (MockDbSetHelper) and a
/// pass-through cache mock. Each handler's happy path, not-found path, and
/// side effects (cache invalidation, event publishing).
/// </summary>
public class JobApplicationHandlersTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<IMessagePublisher> _mockPublisher;

    public JobApplicationHandlersTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCache = new Mock<ICacheService>();
        _mockPublisher = new Mock<IMessagePublisher>();

        // Cache-aside: run the factory inline so the handler's real query
        // logic executes against the mocked DbSet.
        _mockCache.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<PagedResult<JobApplicationDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string k, Func<CancellationToken, Task<PagedResult<JobApplicationDto>>> f,
                TimeSpan? e, IEnumerable<string>? t, CancellationToken ct) => f(ct));
    }

    private void SeedJobApplications(params JobApplication[] apps)
    {
        _mockContext.Setup(x => x.JobApplications)
            .Returns(MockDbSetHelper.Create(apps).Object);
    }

    private static JobApplication App(string company, JobStatus status = JobStatus.Draft)
    {
        var app = new JobApplication(company, "Dev", Guid.NewGuid());
        if (status != JobStatus.Draft)
        {
            app.UpdateStatus(status);
        }
        return app;
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResults()
    {
        SeedJobApplications(App("Acme"), App("Globex"), App("Initech"));
        var handler = new GetAllJobApplicationsQueryHandler(_mockContext.Object, _mockCache.Object);

        var result = await handler.Handle(
            new GetAllJobApplicationsQuery(Page: 1, PageSize: 2), CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_Filters()
    {
        SeedJobApplications(App("Acme", JobStatus.Applied), App("Globex", JobStatus.Draft));
        var handler = new GetAllJobApplicationsQueryHandler(_mockContext.Object, _mockCache.Object);

        var result = await handler.Handle(
            new GetAllJobApplicationsQuery(Status: JobStatus.Applied), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().CompanyName.Should().Be("Acme");
    }

    [Fact]
    public async Task GetAll_WithSearchTerm_FiltersByCompanyOrTitle()
    {
        SeedJobApplications(App("Acme"), App("Globex"));
        var handler = new GetAllJobApplicationsQueryHandler(_mockContext.Object, _mockCache.Object);

        var result = await handler.Handle(
            new GetAllJobApplicationsQuery(SearchTerm: "acme"), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().CompanyName.Should().Be("Acme");
    }

    [Fact]
    public async Task GetJobApplication_Existing_ReturnsDto()
    {
        var app = App("Acme");
        SeedJobApplications(app);
        var handler = new GetJobApplicationQueryHandler(_mockContext.Object);

        var dto = await handler.Handle(
            new GetJobApplicationQuery(app.Id), CancellationToken.None);

        dto.CompanyName.Should().Be("Acme");
        dto.Id.Should().Be(app.Id);
    }

    [Fact]
    public async Task GetJobApplication_Missing_ThrowsKeyNotFound()
    {
        SeedJobApplications();
        var handler = new GetJobApplicationQueryHandler(_mockContext.Object);

        var act = () => handler.Handle(
            new GetJobApplicationQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateJobApplication_Existing_UpdatesAndInvalidatesCache()
    {
        var app = App("Old");
        SeedJobApplications(app);
        var handler = new UpdateJobApplicationCommandHandler(_mockContext.Object, _mockCache.Object);

        await handler.Handle(
            new UpdateJobApplicationCommand(app.Id, "New", "Senior", null, 70000m),
            CancellationToken.None);

        app.CompanyName.Should().Be("New");
        app.PositionTitle.Should().Be("Senior");
        _mockCache.Verify(x => x.RemoveByTagAsync("jobapps", It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateJobApplication_Missing_ThrowsKeyNotFound()
    {
        SeedJobApplications();
        var handler = new UpdateJobApplicationCommandHandler(_mockContext.Object, _mockCache.Object);

        var act = () => handler.Handle(
            new UpdateJobApplicationCommand(Guid.NewGuid(), "New", "Senior", null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteJobApplication_Existing_RemovesAndInvalidatesCache()
    {
        var app = App("Acme");
        var mockSet = MockDbSetHelper.Create(new[] { app });
        _mockContext.Setup(x => x.JobApplications).Returns(mockSet.Object);
        var handler = new DeleteJobApplicationCommandHandler(_mockContext.Object, _mockCache.Object);

        await handler.Handle(new DeleteJobApplicationCommand(app.Id), CancellationToken.None);

        mockSet.Verify(x => x.Remove(app), Times.Once);
        _mockCache.Verify(x => x.RemoveByTagAsync("jobapps", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteJobApplication_Missing_ThrowsKeyNotFound()
    {
        SeedJobApplications();
        var handler = new DeleteJobApplicationCommandHandler(_mockContext.Object, _mockCache.Object);

        var act = () => handler.Handle(
            new DeleteJobApplicationCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateStatus_Existing_UpdatesAndPublishesEvent()
    {
        var app = App("Acme");
        SeedJobApplications(app);
        var handler = new UpdateJobApplicationStatusCommandHandler(
            _mockContext.Object, _mockPublisher.Object);

        await handler.Handle(
            new UpdateJobApplicationStatusCommand(app.Id, JobStatus.Interviewing),
            CancellationToken.None);

        app.Status.Should().Be(JobStatus.Interviewing);
        _mockPublisher.Verify(x => x.PublishAsync(
            It.Is<JobApplicationStatusChangedEvent>(e =>
                e.JobApplicationId == app.Id && e.NewStatus == JobStatus.Interviewing),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_Missing_ThrowsKeyNotFound()
    {
        SeedJobApplications();
        var handler = new UpdateJobApplicationStatusCommandHandler(
            _mockContext.Object, _mockPublisher.Object);

        var act = () => handler.Handle(
            new UpdateJobApplicationStatusCommand(Guid.NewGuid(), JobStatus.Applied),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _mockPublisher.Verify(x => x.PublishAsync(
            It.IsAny<JobApplicationStatusChangedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
