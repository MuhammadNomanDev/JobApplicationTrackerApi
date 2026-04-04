using JobApplicationTracker.Application.Features.Notes.Commands;
using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JobApplicationTracker.Application.Features.Notes.Handlers;

public class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateNoteCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
        var jobApplication = await _context.JobApplications
            .FirstOrDefaultAsync(j => j.Id == request.JobApplicationId, cancellationToken);

        if (jobApplication == null)
        {
            throw new KeyNotFoundException($"Job application with ID {request.JobApplicationId} not found.");
        }

        var note = new Note(request.Content, request.JobApplicationId);

        await _context.Notes.AddAsync(note, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return note.Id;
    }
}
