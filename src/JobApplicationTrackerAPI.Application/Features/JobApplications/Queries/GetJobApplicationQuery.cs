using JobApplicationTracker.Application.Features.JobApplications.Responses;
using MediatR;

namespace JobApplicationTracker.Application.Features.JobApplications.Queries;

public record GetJobApplicationQuery(Guid Id) : IRequest<JobApplicationDto>;
