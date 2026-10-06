using MediatR;

namespace JobApplicationTrackerAPI.Application.Features.Documents.Commands;

public record DeleteDocumentCommand(Guid Id) : IRequest;
