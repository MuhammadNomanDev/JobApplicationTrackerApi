using MediatR;

namespace JobApplicationTracker.Application.Features.JobApplications.Commands;

public record DeleteJobApplicationCommand(Guid Id) : IRequest;
