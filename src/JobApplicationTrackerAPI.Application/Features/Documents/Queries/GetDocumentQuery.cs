using JobApplicationTrackerAPI.Application.Features.Documents.Responses;
using MediatR;

namespace JobApplicationTrackerAPI.Application.Features.Documents.Queries;

public record GetDocumentQuery(Guid Id) : IRequest<DocumentDto>;
