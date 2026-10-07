using System.ComponentModel.DataAnnotations;
using JobApplicationTrackerAPI.Application.Events;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Commands;
using JobApplicationTrackerAPI.Application.Interfaces;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTrackerAPI.Application.Features.JobApplications.Handlers;

public class UpdateJobApplicationStatusCommandHandler : IRequestHandler<UpdateJobApplicationStatusCommand>
{
    private readonly IAppDbContext _context;
    private readonly IMessagePublisher _messagePublisher;

    public UpdateJobApplicationStatusCommandHandler(IAppDbContext context, IMessagePublisher messagePublisher)
    {
        _context = context;
        _messagePublisher = messagePublisher;
    }

    public async Task Handle(UpdateJobApplicationStatusCommand request, CancellationToken cancellationToken)
    {
        var jobApplication = await _context.JobApplications
            .FirstOrDefaultAsync(j => j.Id == request.Id, cancellationToken);

        if (jobApplication == null)
        {
            throw new KeyNotFoundException($"Job application with ID {request.Id} not found.");
        }

        var oldStatus = jobApplication.Status;
        jobApplication.UpdateStatus(request.Status);

        await _context.SaveChangesAsync(cancellationToken);

        // Publish status changed event
        var @event = new JobApplicationStatusChangedEvent(
            jobApplication.Id,
            jobApplication.UserId,
            jobApplication.CompanyName,
            jobApplication.PositionTitle,
            oldStatus,
            jobApplication.Status,
            DateTime.UtcNow);

        await _messagePublisher.PublishAsync(@event, cancellationToken);
    }
}
