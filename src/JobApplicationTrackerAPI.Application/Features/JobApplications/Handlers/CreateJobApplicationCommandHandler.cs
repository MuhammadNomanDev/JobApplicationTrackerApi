using JobApplicationTracker.Application.Features.JobApplications.Commands;
using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Application.Interfaces.Services;
using JobApplicationTracker.Domain.Entities;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace JobApplicationTracker.Application.Features.JobApplications.Handlers;

public class CreateJobApplicationCommandHandler : IRequestHandler<CreateJobApplicationCommand, Guid>
{
    private readonly IAppDbContext _context;
    private readonly ICacheService _cacheService;

    public CreateJobApplicationCommandHandler(IAppDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<Guid> Handle(CreateJobApplicationCommand request, CancellationToken cancellationToken)
    {
        // TODO: Get current user ID from claims (for now, we'll use a placeholder)
        // In production, this should come from the JWT token claims
        var userId = GetCurrentUserId();

        var jobApplication = new JobApplication(
            request.CompanyName,
            request.PositionTitle,
            userId);

        jobApplication.UpdateDetails(
            request.CompanyName,
            request.PositionTitle,
            request.JobUrl,
            request.Salary);

        if (request.Status != Domain.Enums.JobStatus.Draft)
        {
            jobApplication.UpdateStatus(request.Status);
        }

        if (request.AppliedDate.HasValue)
        {
            // AppliedDate is set automatically when status changes to Applied
        }

        await _context.JobApplications.AddAsync(jobApplication, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveByPrefixAsync("jobapps:", cancellationToken);

        return jobApplication.Id;
    }

    private static Guid GetCurrentUserId()
    {
        // TODO: Extract from HttpContext.User claims
        return Guid.Empty;
    }
}
