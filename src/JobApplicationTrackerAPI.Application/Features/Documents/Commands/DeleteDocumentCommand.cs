using MediatR;

namespace JobApplicationTracker.Application.Features.Documents.Commands;

public record DeleteDocumentCommand(Guid Id) : IRequest;
