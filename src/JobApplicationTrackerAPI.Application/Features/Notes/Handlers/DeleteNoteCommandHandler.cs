using JobApplicationTracker.Application.Features.Notes.Commands;
using JobApplicationTracker.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JobApplicationTracker.Application.Features.Notes.Handlers;

public class DeleteNoteCommandHandler : IRequestHandler<DeleteNoteCommand>
{
    private readonly IAppDbContext _context;

    public DeleteNoteCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await _context.Notes
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken);

        if (note == null)
        {
            throw new KeyNotFoundException($"Note with ID {request.Id} not found.");
        }

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
