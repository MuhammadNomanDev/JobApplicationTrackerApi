using JobApplicationTrackerAPI.Domain.Enums;

namespace JobApplicationTrackerAPI.Application.Features.JobApplications.Responses;

public record JobApplicationDto(
    Guid Id,
    string CompanyName,
    string PositionTitle,
    string? JobUrl,
    decimal? Salary,
    JobStatus Status,
    DateTime? AppliedDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
