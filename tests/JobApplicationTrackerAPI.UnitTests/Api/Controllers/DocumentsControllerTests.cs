using AwesomeAssertions;
using JobApplicationTrackerAPI.Api.Controllers;
using JobApplicationTrackerAPI.Api.Models;
using JobApplicationTrackerAPI.Application.Features.Documents.Commands;
using JobApplicationTrackerAPI.Application.Features.Documents.Queries;
using JobApplicationTrackerAPI.Application.Features.Documents.Responses;
using JobApplicationTrackerAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Api.Controllers;

/// <summary>
/// P1/M1j: documents controller tests.
/// </summary>
public class DocumentsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly DocumentsController _sut;

    public DocumentsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _sut = new DocumentsController(_mockMediator.Object);
    }

    private static DocumentDto Dto()
        => new(Guid.NewGuid(), "cv.pdf", "https://x/cv.pdf", null,
            DocumentType.CV, Guid.NewGuid(), DateTime.UtcNow, null);

    [Fact]
    public async Task GetDocument_ReturnsOk()
    {
        var dto = Dto();
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetDocumentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _sut.GetDocument(dto.Id);

        _mockMediator.Verify(m => m.Send(
            It.Is<GetDocumentQuery>(q => q.Id == dto.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<ApiResponse<DocumentDto>>()
            .Which.Data.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetDocumentsByJobApplication_ReturnsOk()
    {
        var jobAppId = Guid.NewGuid();
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetDocumentsByJobApplicationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.GetDocumentsByJobApplication(jobAppId);

        _mockMediator.Verify(m => m.Send(
            It.Is<GetDocumentsByJobApplicationQuery>(q => q.JobApplicationId == jobAppId),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UploadDocument_BuildsCommand_AndReturns201()
    {
        var jobAppId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns("cv.pdf");
        _mockMediator
            .Setup(m => m.Send(It.IsAny<CreateDocumentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(id);

        var result = await _sut.UploadDocument(jobAppId, file.Object, DocumentType.CV);

        _mockMediator.Verify(m => m.Send(
            It.Is<CreateDocumentCommand>(c =>
                c.JobApplicationId == jobAppId &&
                c.File == file.Object &&
                c.DocumentType == DocumentType.CV),
            It.IsAny<CancellationToken>()), Times.Once);
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.ActionName.Should().Be(nameof(DocumentsController.GetDocument));
    }

    [Fact]
    public async Task DeleteDocument_Returns204()
    {
        var id = Guid.NewGuid();
        _mockMediator
            .Setup(m => m.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteDocument(id);

        _mockMediator.Verify(m => m.Send(
            It.Is<DeleteDocumentCommand>(c => c.Id == id),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<NoContentResult>();
    }
}
