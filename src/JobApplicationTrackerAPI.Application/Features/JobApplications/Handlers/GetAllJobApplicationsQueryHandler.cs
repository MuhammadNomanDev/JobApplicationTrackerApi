using JobApplicationTracker.Application.Features.JobApplications.Queries;
using JobApplicationTracker.Application.Features.JobApplications.Responses;
using JobApplicationTracker.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Application.Features.JobApplications.Handlers;

public class GetAllJobApplicationsQueryHandler : IRequestHandler<GetAllJobApplicationsQuery, PagedResult<JobApplicationDto>>
{
    private readonly IAppDbContext _context;

    public GetAllJobApplicationsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<JobApplicationDto>> Handle(GetAllJobApplicationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.JobApplications.AsQueryable();

        // Filter by status if provided
        if (request.Status.HasValue)
        {
            query = query.Where(j => j.Status == request.Status.Value);
        }

        // Search by company name or position title
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(j =>
                j.CompanyName.ToLower().Contains(searchTerm) ||
                j.PositionTitle.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(j => new JobApplicationDto(
                j.Id,
                j.CompanyName,
                j.PositionTitle,
                j.JobUrl,
                j.Salary,
                j.Status,
                j.AppliedDate,
                j.CreatedAt,
                j.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<JobApplicationDto>(items, request.Page, request.PageSize, totalCount);
    }
}
