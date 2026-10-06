using JobApplicationTrackerAPI.Application.Features.Documents.Responses;
using MediatR;

namespace JobApplicationTrackerAPI.Application.Features.Documents.Queries;

public record GetDocumentsByJobApplicationQuery(Guid JobApplicationId) : IRequest<IEnumerable<DocumentDto>>;
