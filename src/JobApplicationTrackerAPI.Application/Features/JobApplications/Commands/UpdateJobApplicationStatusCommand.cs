using FluentValidation;
using JobApplicationTrackerAPI.Domain.Enums;
using MediatR;

namespace JobApplicationTrackerAPI.Application.Features.JobApplications.Commands;

public record UpdateJobApplicationStatusCommand(
    Guid Id,
    JobStatus Status) : IRequest;

public class UpdateJobApplicationStatusCommandValidator : AbstractValidator<UpdateJobApplicationStatusCommand>
{
    public UpdateJobApplicationStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Job application ID is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid job status value.");
    }
}
