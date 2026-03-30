namespace JobApplicationTracker.Application.Features.Auth.Responses;

public record AuthResponse(
    string Token,
    string RefreshToken,
    DateTime Expiration);
