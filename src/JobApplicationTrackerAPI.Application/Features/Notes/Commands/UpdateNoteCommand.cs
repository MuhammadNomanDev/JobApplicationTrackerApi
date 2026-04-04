using FluentValidation;
using MediatR;

namespace JobApplicationTracker.Application.Features.Notes.Commands;

public record UpdateNoteCommand(
    Guid Id,
    string Content) : IRequest;

public class UpdateNoteCommandValidator : AbstractValidator<UpdateNoteCommand>
{
    public UpdateNoteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Note ID is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Note content is required.")
            .MaximumLength(2000).WithMessage("Note content must not exceed 2000 characters.");
    }
}
