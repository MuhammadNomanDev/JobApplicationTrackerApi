using AwesomeAssertions;
using JobApplicationTrackerAPI.Application.Features.Documents.Commands;
using JobApplicationTrackerAPI.Application.Features.Documents.Handlers;
using JobApplicationTrackerAPI.Application.Features.Documents.Queries;
using JobApplicationTrackerAPI.Application.Interfaces;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Domain.Entities;
using JobApplicationTrackerAPI.Domain.Enums;
using JobApplicationTrackerAPI.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Features.Documents;

/// <summary>
/// P1/M1h: document handler unit tests with a mocked blob storage service.
/// </summary>
public class DocumentHandlersTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IBlobStorageService> _mockBlob;

    public DocumentHandlersTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockBlob = new Mock<IBlobStorageService>();
    }

    private static Mock<IFormFile> File(string fileName = "cv.pdf")
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns("application/pdf");
        file.Setup(f => f.Length).Returns(1024);
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
        return file;
    }

    [Fact]
    public async Task CreateDocument_ExistingJobApplication_UploadsAndReturnsId()
    {
        var app = new JobApplication("Acme", "Dev", Guid.NewGuid());
        _mockContext.Setup(x => x.JobApplications)
            .Returns(MockDbSetHelper.Create(new[] { app }).Object);
        _mockContext.Setup(x => x.Documents)
            .Returns(MockDbSetHelper.Create(Array.Empty<Document>()).Object);
        _mockBlob.Setup(x => x.UploadAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://blobs/abc_cv.pdf");
        var handler = new CreateDocumentCommandHandler(_mockContext.Object, _mockBlob.Object);

        var id = await handler.Handle(
            new CreateDocumentCommand(app.Id, File().Object, DocumentType.CV),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        _mockBlob.Verify(x => x.UploadAsync(
            It.IsAny<Stream>(),
            It.Is<string>(n => n.EndsWith("_cv.pdf")),
            "application/pdf",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDocument_MissingJobApplication_ThrowsKeyNotFound()
    {
        _mockContext.Setup(x => x.JobApplications)
            .Returns(MockDbSetHelper.Create(Array.Empty<JobApplication>()).Object);
        var handler = new CreateDocumentCommandHandler(_mockContext.Object, _mockBlob.Object);

        var act = () => handler.Handle(
            new CreateDocumentCommand(Guid.NewGuid(), File().Object, DocumentType.CV),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _mockBlob.Verify(x => x.UploadAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteDocument_Existing_DeletesBlobAndRemoves()
    {
        var doc = new Document("cv.pdf", "https://blobs/abc.pdf", DocumentType.CV, Guid.NewGuid());
        var mockSet = MockDbSetHelper.Create(new[] { doc });
        _mockContext.Setup(x => x.Documents).Returns(mockSet.Object);
        var handler = new DeleteDocumentCommandHandler(_mockContext.Object, _mockBlob.Object);

        await handler.Handle(new DeleteDocumentCommand(doc.Id), CancellationToken.None);

        _mockBlob.Verify(x => x.DeleteAsync("https://blobs/abc.pdf", It.IsAny<CancellationToken>()), Times.Once);
        mockSet.Verify(x => x.Remove(doc), Times.Once);
    }

    [Fact]
    public async Task DeleteDocument_Missing_ThrowsKeyNotFound()
    {
        _mockContext.Setup(x => x.Documents)
            .Returns(MockDbSetHelper.Create(Array.Empty<Document>()).Object);
        var handler = new DeleteDocumentCommandHandler(_mockContext.Object, _mockBlob.Object);

        var act = () => handler.Handle(
            new DeleteDocumentCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetDocument_Existing_ReturnsDtoWithSasUrl()
    {
        var doc = new Document("cv.pdf", "https://blobs/abc.pdf", DocumentType.CV, Guid.NewGuid());
        _mockContext.Setup(x => x.Documents)
            .Returns(MockDbSetHelper.Create(new[] { doc }).Object);
        _mockBlob.Setup(x => x.GetSasUrlAsync(
                "https://blobs/abc.pdf", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://blobs/abc.pdf?sig=xyz");
        var handler = new GetDocumentQueryHandler(_mockContext.Object, _mockBlob.Object);

        var dto = await handler.Handle(new GetDocumentQuery(doc.Id), CancellationToken.None);

        dto.FileName.Should().Be("cv.pdf");
        dto.SasUrl.Should().Be("https://blobs/abc.pdf?sig=xyz");
    }

    [Fact]
    public async Task GetDocument_Missing_ThrowsKeyNotFound()
    {
        _mockContext.Setup(x => x.Documents)
            .Returns(MockDbSetHelper.Create(Array.Empty<Document>()).Object);
        var handler = new GetDocumentQueryHandler(_mockContext.Object, _mockBlob.Object);

        var act = () => handler.Handle(
            new GetDocumentQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetDocumentsByJobApplication_ReturnsOnlyMatching()
    {
        var jobAppId = Guid.NewGuid();
        _mockContext.Setup(x => x.Documents)
            .Returns(MockDbSetHelper.Create(new[]
            {
                new Document("a.pdf", "https://x/a.pdf", DocumentType.CV, jobAppId),
                new Document("b.pdf", "https://x/b.pdf", DocumentType.CoverLetter, Guid.NewGuid()),
            }).Object);
        var handler = new GetDocumentsByJobApplicationQueryHandler(_mockContext.Object);

        var result = (await handler.Handle(
            new GetDocumentsByJobApplicationQuery(jobAppId), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result.Single().FileName.Should().Be("a.pdf");
    }
}
