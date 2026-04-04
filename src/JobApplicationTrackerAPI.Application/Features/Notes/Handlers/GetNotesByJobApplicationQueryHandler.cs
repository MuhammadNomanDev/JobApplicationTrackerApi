using JobApplicationTracker.Application.Features.Notes.Queries;
using JobApplicationTracker.Application.Features.Notes.Responses;
using JobApplicationTracker.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Application.Features.Notes.Handlers;

public class GetNotesByJobApplicationQueryHandler : IRequestHandler<GetNotesByJobApplicationQuery, IEnumerable<NoteDto>>
{
    private readonly IAppDbContext _context;

    public GetNotesByJobApplicationQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<NoteDto>> Handle(GetNotesByJobApplicationQuery request, CancellationToken cancellationToken)
    {
        return await _context.Notes
            .Where(n => n.JobApplicationId == request.JobApplicationId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoteDto(
                n.Id,
                n.Content,
                n.JobApplicationId,
                n.CreatedAt,
                n.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
