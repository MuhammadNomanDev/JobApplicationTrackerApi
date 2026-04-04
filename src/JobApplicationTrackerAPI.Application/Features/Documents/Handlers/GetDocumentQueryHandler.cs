using JobApplicationTracker.Application.Features.Documents.Queries;
using JobApplicationTracker.Application.Features.Documents.Responses;
using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JobApplicationTracker.Application.Features.Documents.Handlers;

public class GetDocumentQueryHandler : IRequestHandler<GetDocumentQuery, DocumentDto>
{
    private readonly IAppDbContext _context;
    private readonly IBlobStorageService _blobStorageService;

    public GetDocumentQueryHandler(IAppDbContext context, IBlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    public async Task<DocumentDto> Handle(GetDocumentQuery request, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (document == null)
        {
            throw new KeyNotFoundException($"Document with ID {request.Id} not found.");
        }

        var sasUrl = await _blobStorageService.GetSasUrlAsync(document.FileUrl, TimeSpan.FromMinutes(15), cancellationToken);

        return MapToDto(document, sasUrl);
    }

    private static DocumentDto MapToDto(Domain.Entities.Document document, string? sasUrl = null) =>
        new(
            document.Id,
            document.FileName,
            document.FileUrl,
            sasUrl,
            document.DocumentType,
            document.JobApplicationId,
            document.CreatedAt,
            document.UpdatedAt);
}
