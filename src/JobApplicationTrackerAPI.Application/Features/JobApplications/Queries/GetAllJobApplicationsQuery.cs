using JobApplicationTrackerAPI.Application.Features.JobApplications.Responses;
using JobApplicationTrackerAPI.Domain.Enums;
using MediatR;

namespace JobApplicationTrackerAPI.Application.Features.JobApplications.Queries;

public record GetAllJobApplicationsQuery(
    int Page = 1,
    int PageSize = 10,
    JobStatus? Status = null,
    string? SearchTerm = null) : IRequest<PagedResult<JobApplicationDto>>;
