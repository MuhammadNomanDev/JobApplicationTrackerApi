using JobApplicationTrackerAPI.Domain.Enums;

namespace JobApplicationTrackerAPI.Application.Events;

public record JobApplicationStatusChangedEvent(
    Guid JobApplicationId,
    Guid UserId,
    string CompanyName,
    string PositionTitle,
    JobStatus OldStatus,
    JobStatus NewStatus,
    DateTime ChangedAt);
