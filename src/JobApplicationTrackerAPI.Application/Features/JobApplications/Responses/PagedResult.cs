namespace JobApplicationTracker.Application.Features.JobApplications.Responses;

public record PagedResult<T>(
    IEnumerable<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
