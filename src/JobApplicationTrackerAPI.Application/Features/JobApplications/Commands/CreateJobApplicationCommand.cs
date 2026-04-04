using FluentValidation;
using JobApplicationTracker.Domain.Enums;
using MediatR;

namespace JobApplicationTracker.Application.Features.JobApplications.Commands;

public record CreateJobApplicationCommand(
    string CompanyName,
    string PositionTitle,
    string? JobUrl,
    decimal? Salary,
    JobStatus Status,
    DateTime? AppliedDate) : IRequest<Guid>;

public class CreateJobApplicationCommandValidator : AbstractValidator<CreateJobApplicationCommand>
{
    public CreateJobApplicationCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.");

        RuleFor(x => x.PositionTitle)
            .NotEmpty().WithMessage("Position title is required.")
            .MaximumLength(200).WithMessage("Position title must not exceed 200 characters.");

        RuleFor(x => x.JobUrl)
            .MaximumLength(500).WithMessage("Job URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.JobUrl));

        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0).WithMessage("Salary must be a positive value.")
            .When(x => x.Salary.HasValue);
    }
}
