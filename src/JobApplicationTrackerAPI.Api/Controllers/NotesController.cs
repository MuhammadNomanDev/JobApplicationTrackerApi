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

}
