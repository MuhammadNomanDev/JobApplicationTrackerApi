using JobApplicationTracker.Application.Features.JobApplications.Responses;
using JobApplicationTracker.Domain.Enums;
using MediatR;

namespace JobApplicationTracker.Application.Features.JobApplications.Queries;

public record GetAllJobApplicationsQuery(
    int Page = 1,
    int PageSize = 10,
    JobStatus? Status = null,
    string? SearchTerm = null) : IRequest<PagedResult<JobApplicationDto>>;
