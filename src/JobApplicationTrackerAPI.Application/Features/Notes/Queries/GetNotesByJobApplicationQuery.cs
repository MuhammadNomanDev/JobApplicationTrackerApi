using JobApplicationTrackerAPI.Application.Features.Notes.Responses;
using MediatR;

namespace JobApplicationTrackerAPI.Application.Features.Notes.Queries;

public record GetNotesByJobApplicationQuery(Guid JobApplicationId) : IRequest<IEnumerable<NoteDto>>;
