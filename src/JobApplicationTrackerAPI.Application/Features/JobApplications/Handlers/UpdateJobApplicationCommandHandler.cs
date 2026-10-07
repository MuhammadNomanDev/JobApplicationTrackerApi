using System.ComponentModel.DataAnnotations;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Commands;
using JobApplicationTrackerAPI.Application.Interfaces;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTrackerAPI.Application.Features.JobApplications.Handlers;

public class UpdateJobApplicationCommandHandler : IRequestHandler<UpdateJobApplicationCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICacheService _cacheService;

    public UpdateJobApplicationCommandHandler(IAppDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task Handle(UpdateJobApplicationCommand request, CancellationToken cancellationToken)
    {
        var jobApplication = await _context.JobApplications
            .FirstOrDefaultAsync(j => j.Id == request.Id, cancellationToken);

        if (jobApplication == null)
        {
            throw new KeyNotFoundException($"Job application with ID {request.Id} not found.");
        }

        jobApplication.UpdateDetails(
            request.CompanyName,
            request.PositionTitle,
            request.JobUrl,
            request.Salary);

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveByTagAsync("jobapps", cancellationToken);
    }
}
