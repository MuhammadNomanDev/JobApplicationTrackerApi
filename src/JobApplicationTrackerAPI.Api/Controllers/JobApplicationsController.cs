using JobApplicationTracker.Api.Models;
using JobApplicationTracker.Application.Features.JobApplications.Commands;
using JobApplicationTracker.Application.Features.JobApplications.Queries;
using JobApplicationTracker.Application.Features.JobApplications.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobApplicationTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobApplicationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all job applications with pagination and optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<JobApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllJobApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null)
    {
        Domain.Enums.JobStatus? jobStatus = null;
        if (Enum.TryParse<Domain.Enums.JobStatus>(status, true, out var parsedStatus))
        {
            jobStatus = parsedStatus;
        }

        var result = await _mediator.Send(new GetAllJobApplicationsQuery(page, pageSize, jobStatus, searchTerm));
        return Ok(ApiResponse<PagedResult<JobApplicationDto>>.Ok(result));
    }

    /// <summary>
    /// Get a job application by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<JobApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobApplication(Guid id)
    {
        var result = await _mediator.Send(new GetJobApplicationQuery(id));
        return Ok(ApiResponse<JobApplicationDto>.Ok(result));
    }

    /// <summary>
    /// Create a new job application
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateJobApplication([FromBody] CreateJobApplicationCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetJobApplication), new { id }, ApiResponse<Guid>.Created(id, "Job application created successfully."));
    }

    /// <summary>
    /// Update a job application
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateJobApplication(Guid id, [FromBody] UpdateJobApplicationCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse<object>.Fail("ID in URL does not match ID in body."));
        }

        await _mediator.Send(command);
        return NoContent();
    }
    
}
