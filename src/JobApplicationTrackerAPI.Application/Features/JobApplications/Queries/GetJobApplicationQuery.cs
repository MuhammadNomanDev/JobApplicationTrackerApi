using JobApplicationTrackerAPI.Application.Features.JobApplications.Responses;
using MediatR;

namespace JobApplicationTrackerAPI.Application.Features.JobApplications.Queries;

public record GetJobApplicationQuery(Guid Id) : IRequest<JobApplicationDto>;
