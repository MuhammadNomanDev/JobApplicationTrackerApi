using JobApplicationTracker.Application.Features.Documents.Responses;
using MediatR;

namespace JobApplicationTracker.Application.Features.Documents.Queries;

public record GetDocumentsByJobApplicationQuery(Guid JobApplicationId) : IRequest<IEnumerable<DocumentDto>>;
