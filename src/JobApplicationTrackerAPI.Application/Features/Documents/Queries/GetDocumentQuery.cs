using JobApplicationTracker.Application.Features.Documents.Responses;
using MediatR;

namespace JobApplicationTracker.Application.Features.Documents.Queries;

public record GetDocumentQuery(Guid Id) : IRequest<DocumentDto>;
