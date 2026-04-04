using FluentValidation;
using MediatR;
using JobApplicationTracker.Application.Features.Auth.Responses;

namespace JobApplicationTracker.Application.Features.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password) : IRequest<AuthResponse>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}