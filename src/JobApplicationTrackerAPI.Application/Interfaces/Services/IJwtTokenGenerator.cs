namespace JobApplicationTracker.Application.Interfaces.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(JobApplicationTracker.Domain.Entities.User user);
    string GenerateRefreshToken();
    int TokenExpirationMinutes { get; }
}