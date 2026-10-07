namespace JobApplicationTrackerAPI.Application.Interfaces.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(JobApplicationTrackerAPI.Domain.Entities.User user);
    string GenerateRefreshToken();
    int TokenExpirationMinutes { get; }
}
