using MediatR;

namespace JobApplicationTrackerAPI.Application.Features.Notes.Commands;

public record DeleteNoteCommand(Guid Id) : IRequest;
