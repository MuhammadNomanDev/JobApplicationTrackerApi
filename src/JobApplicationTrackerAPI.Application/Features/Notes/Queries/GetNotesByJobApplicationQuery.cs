using JobApplicationTracker.Application.Features.Notes.Responses;
using MediatR;

namespace JobApplicationTracker.Application.Features.Notes.Queries;

public record GetNotesByJobApplicationQuery(Guid JobApplicationId) : IRequest<IEnumerable<NoteDto>>;
