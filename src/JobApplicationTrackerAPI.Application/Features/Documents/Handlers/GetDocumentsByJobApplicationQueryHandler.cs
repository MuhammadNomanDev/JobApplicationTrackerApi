using JobApplicationTrackerAPI.Application.Features.Documents.Queries;
using JobApplicationTrackerAPI.Application.Features.Documents.Responses;
using JobApplicationTrackerAPI.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTrackerAPI.Application.Features.Documents.Handlers;

public class GetDocumentsByJobApplicationQueryHandler : IRequestHandler<GetDocumentsByJobApplicationQuery, IEnumerable<DocumentDto>>
{
    private readonly IAppDbContext _context;

    public GetDocumentsByJobApplicationQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DocumentDto>> Handle(GetDocumentsByJobApplicationQuery request, CancellationToken cancellationToken)
    {
        return await _context.Documents
            .Where(d => d.JobApplicationId == request.JobApplicationId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DocumentDto(
                d.Id,
                d.FileName,
                d.FileUrl,
                null,
                d.DocumentType,
                d.JobApplicationId,
                d.CreatedAt,
                d.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
