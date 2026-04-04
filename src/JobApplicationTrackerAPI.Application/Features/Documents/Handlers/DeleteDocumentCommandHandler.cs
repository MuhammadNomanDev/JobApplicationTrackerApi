using JobApplicationTracker.Application.Features.Documents.Commands;
using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JobApplicationTracker.Application.Features.Documents.Handlers;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IAppDbContext _context;
    private readonly IBlobStorageService _blobStorageService;

    public DeleteDocumentCommandHandler(IAppDbContext context, IBlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    public async Task Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (document == null)
        {
            throw new KeyNotFoundException($"Document with ID {request.Id} not found.");
        }

        await _blobStorageService.DeleteAsync(document.FileUrl, cancellationToken);

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
