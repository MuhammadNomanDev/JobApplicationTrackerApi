using JobApplicationTracker.Application.Features.JobApplications.Queries;
using JobApplicationTracker.Application.Features.JobApplications.Responses;
using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Application.Features.JobApplications.Handlers;

public class GetAllJobApplicationsQueryHandler : IRequestHandler<GetAllJobApplicationsQuery, PagedResult<JobApplicationDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICacheService _cacheService;
    private const string CacheKeyPrefix = "jobapps:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public GetAllJobApplicationsQueryHandler(IAppDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<PagedResult<JobApplicationDto>> Handle(GetAllJobApplicationsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{request.Page}:{request.PageSize}:{request.Status}:{request.SearchTerm}";

        var cached = await _cacheService.GetAsync<PagedResult<JobApplicationDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var query = _context.JobApplications.AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(j => j.Status == request.Status.Value);
        }

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

        var result = new PagedResult<JobApplicationDto>(items, request.Page, request.PageSize, totalCount);

        await _cacheService.SetAsync(cacheKey, result, CacheTtl, cancellationToken);

        return result;
    }
}
