using MediatR;

namespace JobApplicationTracker.Application.Features.Notes.Commands;

public record DeleteNoteCommand(Guid Id) : IRequest;
