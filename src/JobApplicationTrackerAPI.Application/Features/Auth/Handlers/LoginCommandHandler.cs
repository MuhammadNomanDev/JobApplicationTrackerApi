using JobApplicationTracker.Application.Features.Auth.Commands;
using JobApplicationTracker.Application.Features.Auth.Responses;
using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Application.Interfaces.Services;
using JobApplicationTracker.Domain.Interfaces;
using MediatR;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Application.Features.Auth.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IAppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IAppDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == request.Email, cancellationToken);

        // Use generic error to prevent user enumeration attacks
        if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            throw new ValidationException("Invalid email or password.");
        }

        // Generate JWT tokens
        var token = _jwtTokenGenerator.GenerateToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var expiration = DateTime.UtcNow.AddMinutes(_jwtTokenGenerator.TokenExpirationMinutes);

        // Update user with refresh token
        user.SetRefreshToken(refreshToken, expiration);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(token, refreshToken, expiration);
    }
}
