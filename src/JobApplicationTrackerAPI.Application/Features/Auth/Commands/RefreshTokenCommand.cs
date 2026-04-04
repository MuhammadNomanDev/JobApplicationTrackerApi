using JobApplicationTracker.Application.Features.Auth.Responses;
using MediatR;

namespace JobApplicationTracker.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string Token,
    string RefreshToken) : IRequest<AuthResponse>;