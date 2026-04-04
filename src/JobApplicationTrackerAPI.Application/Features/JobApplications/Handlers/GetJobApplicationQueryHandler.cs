using JobApplicationTracker.Application.Features.JobApplications.Queries;
using JobApplicationTracker.Application.Features.JobApplications.Responses;
using JobApplicationTracker.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JobApplicationTracker.Application.Features.JobApplications.Handlers;

public class GetJobApplicationQueryHandler : IRequestHandler<GetJobApplicationQuery, JobApplicationDto>
{
    private readonly IAppDbContext _context;

    public GetJobApplicationQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<JobApplicationDto> Handle(GetJobApplicationQuery request, CancellationToken cancellationToken)
    {
        var jobApplication = await _context.JobApplications
            .FirstOrDefaultAsync(j => j.Id == request.Id, cancellationToken);

        if (jobApplication == null)
        {
            throw new KeyNotFoundException($"Job application with ID {request.Id} not found.");
        }

        return MapToDto(jobApplication);
    }

    private static JobApplicationDto MapToDto(Domain.Entities.JobApplication jobApplication) =>
        new(
            jobApplication.Id,
            jobApplication.CompanyName,
            jobApplication.PositionTitle,
            jobApplication.JobUrl,
            jobApplication.Salary,
            jobApplication.Status,
            jobApplication.AppliedDate,
            jobApplication.CreatedAt,
            jobApplication.UpdatedAt);
}
