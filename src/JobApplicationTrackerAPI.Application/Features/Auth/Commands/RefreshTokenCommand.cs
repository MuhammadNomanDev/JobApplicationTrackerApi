using FluentValidation;
using JobApplicationTrackerAPI.Application.Features.Auth.Responses;
using MediatR;

namespace JobApplicationTrackerAPI.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string Token,
    string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
