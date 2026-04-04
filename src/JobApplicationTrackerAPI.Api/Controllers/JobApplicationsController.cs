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

    
}
