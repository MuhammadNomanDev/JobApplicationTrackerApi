using JobApplicationTracker.Api.Models;
using JobApplicationTracker.Application.Features.Notes.Commands;
using JobApplicationTracker.Application.Features.Notes.Queries;
using JobApplicationTracker.Application.Features.Notes.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobApplicationTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all notes for a job application
    /// </summary>
    [HttpGet("GetNotesByJobApplication/{jobApplicationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NoteDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotesByJobApplication(Guid jobApplicationId)
    {
        var result = await _mediator.Send(new GetNotesByJobApplicationQuery(jobApplicationId));
        return Ok(ApiResponse<IEnumerable<NoteDto>>.Ok(result));
    }

    /// <summary>
    /// Create a new note for a job application
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateNote([FromBody] CreateNoteCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetNotesByJobApplication), new { jobApplicationId = command.JobApplicationId }, ApiResponse<Guid>.Created(id, "Note created successfully."));
    }
    
}
