using JobApplicationTrackerAPI.Application.Features.Documents.Commands;
using JobApplicationTrackerAPI.Application.Interfaces;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JobApplicationTrackerAPI.Application.Features.Documents.Handlers;

public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, Guid>
{
    private readonly IAppDbContext _context;
    private readonly IBlobStorageService _blobStorageService;

    public CreateDocumentCommandHandler(IAppDbContext context, IBlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    public async Task<Guid> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
    {
        var jobApplication = await _context.JobApplications
            .FirstOrDefaultAsync(j => j.Id == request.JobApplicationId, cancellationToken);

        if (jobApplication == null)
        {
            throw new KeyNotFoundException($"Job application with ID {request.JobApplicationId} not found.");
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{request.File.FileName}";

        await using var stream = request.File.OpenReadStream();
        var blobUrl = await _blobStorageService.UploadAsync(stream, uniqueFileName, request.File.ContentType, cancellationToken);

        var document = new Document(request.File.FileName, blobUrl, request.DocumentType, request.JobApplicationId);

        await _context.Documents.AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return document.Id;
    }
}
