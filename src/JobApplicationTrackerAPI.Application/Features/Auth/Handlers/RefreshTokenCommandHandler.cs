using JobApplicationTracker.Application.Features.Auth.Commands;
using JobApplicationTracker.Application.Features.Auth.Responses;
using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Application.Interfaces.Services;
using JobApplicationTracker.Domain.Entities;
using JobApplicationTracker.Domain.Interfaces;
using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Application.Features.Auth.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IAppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IAppDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find user by refresh token
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null)
        {
            throw new ValidationException("Invalid refresh token.");
        }

        // Check if refresh token is expired
        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new ValidationException("Refresh token has expired.");
        }

        // Generate new JWT tokens
        var token = _jwtTokenGenerator.GenerateToken(user);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var expiration = DateTime.UtcNow.AddMinutes(_jwtTokenGenerator.TokenExpirationMinutes);

        // Update user with new refresh token
        user.SetRefreshToken(newRefreshToken, expiration);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(token, newRefreshToken, expiration);
    }
}