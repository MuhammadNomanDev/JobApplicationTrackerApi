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

    /// <summary>
    /// Get all documents for a job application
    /// </summary>
    [HttpGet("GetDocumentsByJobApplication/{jobApplicationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DocumentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocumentsByJobApplication(Guid jobApplicationId)
    {
        var result = await _mediator.Send(new GetDocumentsByJobApplicationQuery(jobApplicationId));
        return Ok(ApiResponse<IEnumerable<DocumentDto>>.Ok(result));
    }

    /// <summary>
    /// Upload a document for a job application
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] Guid jobApplicationId,
        [FromForm] IFormFile file,
        [FromForm] DocumentType documentType)
    {
        var command = new CreateDocumentCommand(jobApplicationId, file, documentType);
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetDocument), new { id }, ApiResponse<Guid>.Created(id, "Document uploaded successfully."));
    }

}
