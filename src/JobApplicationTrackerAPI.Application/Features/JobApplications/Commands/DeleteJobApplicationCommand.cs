using MediatR;

namespace JobApplicationTrackerAPI.Application.Features.JobApplications.Commands;

public record DeleteJobApplicationCommand(Guid Id) : IRequest;
