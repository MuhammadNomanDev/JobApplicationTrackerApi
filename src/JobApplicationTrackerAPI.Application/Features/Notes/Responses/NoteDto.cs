namespace JobApplicationTracker.Application.Features.Notes.Responses;

public record NoteDto(
    Guid Id,
    string Content,
    Guid JobApplicationId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
