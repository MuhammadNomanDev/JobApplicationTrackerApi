namespace JobApplicationTracker.Application.Features.Auth.Requests;

public record LoginRequest(
    string Email,
    string Password);
