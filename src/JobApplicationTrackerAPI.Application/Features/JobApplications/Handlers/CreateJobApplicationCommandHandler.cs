using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using JobApplicationTrackerAPI.Application.Features.JobApplications.Commands;
using JobApplicationTrackerAPI.Application.Interfaces;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace JobApplicationTrackerAPI.Application.Features.JobApplications.Handlers;

public class CreateJobApplicationCommandHandler : IRequestHandler<CreateJobApplicationCommand, Guid>
{
    private readonly IAppDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateJobApplicationCommandHandler(
        IAppDbContext context,
        ICacheService cacheService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _cacheService = cacheService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Guid> Handle(CreateJobApplicationCommand request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var jobApplication = new JobApplication(
            request.CompanyName,
            request.PositionTitle,
            userId);

        jobApplication.UpdateDetails(
            request.CompanyName,
            request.PositionTitle,
            request.JobUrl,
            request.Salary);

        if (request.Status != Domain.Enums.JobStatus.Draft)
        {
            jobApplication.UpdateStatus(request.Status);
        }

        if (request.AppliedDate.HasValue)
        {
            // AppliedDate is set automatically when status changes to Applied
        }

        await _context.JobApplications.AddAsync(jobApplication, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveByTagAsync("jobapps", cancellationToken);

        return jobApplication.Id;
    }

    private Guid GetCurrentUserId()
    {
        var idValue = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        // The endpoint is [Authorize] and our JWTs always carry the user id
        // (JwtTokenGenerator sets ClaimTypes.NameIdentifier), so a missing or
        // malformed claim means the token itself is bad.
        if (string.IsNullOrEmpty(idValue) || !Guid.TryParse(idValue, out var userId))
        {
            throw new UnauthorizedAccessException("User ID claim is missing from the token.");
        }

        return userId;
    }
}
