using System.ComponentModel.DataAnnotations;
using JobApplicationTracker.Application.Features.Auth.Commands;
using JobApplicationTracker.Application.Features.Auth.Responses;
using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Application.Interfaces.Services;
using JobApplicationTracker.Domain.Entities;
using JobApplicationTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Application.Features.Auth.Handlers;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IAppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(
        IAppDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email.Value == request.Email, cancellationToken))
        {
            throw new ValidationException("Email already exists.");
        }

        // Create new user
        var user = new User(
            request.FirstName,
            request.LastName,
            JobApplicationTracker.Domain.ValueObjects.Email.Create(request.Email),
            _passwordHasher.HashPassword(request.Password)
        );

        // Generate JWT tokens
        var token = _jwtTokenGenerator.GenerateToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var expiration = DateTime.UtcNow.AddMinutes(_jwtTokenGenerator.TokenExpirationMinutes);

        // Update user with refresh token
        user.SetRefreshToken(refreshToken, expiration);

        // Add user to database
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(token, refreshToken, expiration);
    }
}