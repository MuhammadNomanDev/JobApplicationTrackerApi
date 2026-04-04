using FluentValidation;
using MediatR;

namespace JobApplicationTracker.Application.Features.Notes.Commands;

public record CreateNoteCommand(
    Guid JobApplicationId,
    string Content) : IRequest<Guid>;

public class CreateNoteCommandValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteCommandValidator()
    {
        RuleFor(x => x.JobApplicationId)
            .NotEmpty().WithMessage("Job application ID is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Note content is required.")
            .MaximumLength(2000).WithMessage("Note content must not exceed 2000 characters.");
    }
}
