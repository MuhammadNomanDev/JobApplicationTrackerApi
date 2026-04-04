using JobApplicationTracker.Api.Models;
using JobApplicationTracker.Application.Features.Documents.Commands;
using JobApplicationTracker.Application.Features.Documents.Queries;
using JobApplicationTracker.Application.Features.Documents.Responses;
using JobApplicationTracker.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobApplicationTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get a document by ID (returns SAS URL for download)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        var result = await _mediator.Send(new GetDocumentQuery(id));
        return Ok(ApiResponse<DocumentDto>.Ok(result));
    }

}
