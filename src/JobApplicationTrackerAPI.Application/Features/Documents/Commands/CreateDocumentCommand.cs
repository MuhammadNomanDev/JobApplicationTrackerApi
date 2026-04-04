using FluentValidation;
using JobApplicationTracker.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace JobApplicationTracker.Application.Features.Documents.Commands;

public record CreateDocumentCommand(
    Guid JobApplicationId,
    IFormFile File,
    DocumentType DocumentType) : IRequest<Guid>;

public class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    private static readonly string[] AllowedContentTypes = {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "image/jpeg",
        "image/png"
    };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public CreateDocumentCommandValidator()
    {
        RuleFor(x => x.JobApplicationId)
            .NotEmpty().WithMessage("Job application ID is required.");

        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.")
            .Must(f => f.Length <= MaxFileSize).WithMessage($"File size must not exceed {MaxFileSize / (1024 * 1024)}MB.")
            .Must(f => AllowedContentTypes.Contains(f.ContentType)).WithMessage("File type is not allowed.");

        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("Invalid document type.");
    }
}
