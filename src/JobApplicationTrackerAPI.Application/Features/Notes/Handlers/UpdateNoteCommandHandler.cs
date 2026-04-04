using JobApplicationTracker.Application.Features.Notes.Commands;
using JobApplicationTracker.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JobApplicationTracker.Application.Features.Notes.Handlers;

public class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand>
{
    private readonly IAppDbContext _context;

    public UpdateNoteCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await _context.Notes
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken);

        if (note == null)
        {
            throw new KeyNotFoundException($"Note with ID {request.Id} not found.");
        }

        note.UpdateContent(request.Content);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
