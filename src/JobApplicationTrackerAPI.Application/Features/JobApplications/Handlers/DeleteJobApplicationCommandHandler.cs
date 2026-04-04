using JobApplicationTracker.Application.Features.JobApplications.Commands;
using JobApplicationTracker.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JobApplicationTracker.Application.Features.JobApplications.Handlers;

public class DeleteJobApplicationCommandHandler : IRequestHandler<DeleteJobApplicationCommand>
{
    private readonly IAppDbContext _context;

    public DeleteJobApplicationCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteJobApplicationCommand request, CancellationToken cancellationToken)
    {
        var jobApplication = await _context.JobApplications
            .FirstOrDefaultAsync(j => j.Id == request.Id, cancellationToken);

        if (jobApplication == null)
        {
            throw new KeyNotFoundException($"Job application with ID {request.Id} not found.");
        }

        _context.JobApplications.Remove(jobApplication);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
