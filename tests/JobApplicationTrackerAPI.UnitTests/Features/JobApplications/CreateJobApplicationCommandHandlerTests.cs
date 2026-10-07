using System.Security.Claims;
using AwesomeAssertions;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Commands;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Handlers;
using JobApplicationTrackerAPI.Application.Interfaces;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Domain.Entities;
using JobApplicationTrackerAPI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
        // IAppDbContext.JobApplications must return a mock DbSet; otherwise the
        // handler's AddAsync call throws NullReferenceException.
        _mockContext.Setup(x => x.JobApplications).Returns(new Mock<DbSet<JobApplication>>().Object);

        // The handler reads the user id from the JWT's NameIdentifier claim.
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }, "TestAuth"));
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        _handler = new CreateJobApplicationCommandHandler(
            _mockContext.Object, _mockCacheService.Object, httpContextAccessor.Object);
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
        _mockContext.Verify(x => x.JobApplications.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByTagAsync("jobapps", It.IsAny<CancellationToken>()), Times.Once);
    }
}
