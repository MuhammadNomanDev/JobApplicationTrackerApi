using JobApplicationTracker.Domain.Enums;

namespace JobApplicationTracker.Application.Features.Documents.Responses;

public record DocumentDto(
    Guid Id,
    string FileName,
    string FileUrl,
    string? SasUrl,
    DocumentType DocumentType,
    Guid JobApplicationId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
