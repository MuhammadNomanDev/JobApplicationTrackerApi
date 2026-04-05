using JobApplicationTracker.Domain.Enums;

namespace JobApplicationTracker.Application.Events;

public record JobApplicationStatusChangedEvent(
    Guid JobApplicationId,
    Guid UserId,
    string CompanyName,
    string PositionTitle,
    JobStatus OldStatus,
    JobStatus NewStatus,
    DateTime ChangedAt);
