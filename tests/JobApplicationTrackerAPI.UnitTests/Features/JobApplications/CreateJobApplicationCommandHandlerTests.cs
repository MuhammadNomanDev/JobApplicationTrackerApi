using FluentAssertions;
using JobApplicationTracker.Application.Features.JobApplications.Commands;
using JobApplicationTracker.Application.Features.JobApplications.Handlers;
using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Application.Interfaces.Services;
using JobApplicationTracker.Domain.Enums;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Features.JobApplications;

public class CreateJobApplicationCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly CreateJobApplicationCommandHandler _handler;

    public CreateJobApplicationCommandHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCacheService = new Mock<ICacheService>();
        _handler = new CreateJobApplicationCommandHandler(_mockContext.Object, _mockCacheService.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnJobApplicationId()
    {
        // Arrange
        var command = new CreateJobApplicationCommand(
            "Test Company",
            "Software Engineer",
            "https://example.com/job",
            100000m,
            JobStatus.Applied,
            DateTime.UtcNow);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _mockContext.Verify(x => x.JobApplications.AddAsync(It.IsAny<JobApplicationTracker.Domain.Entities.JobApplication>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPrefixAsync("jobapps:", It.IsAny<CancellationToken>()), Times.Once);
    }
}
