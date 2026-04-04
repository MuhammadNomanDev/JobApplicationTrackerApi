using JobApplicationTracker.Api.Models;
using JobApplicationTracker.Application.Features.Auth.Commands;
using JobApplicationTracker.Application.Features.Auth.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JobApplicationTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var response = await _mediator.Send(command);
        return Ok(ApiResponse<AuthResponse>.Created(response, "User registered successfully."));
    }

    /// <summary>
    /// Authenticate user and return JWT tokens
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var response = await _mediator.Send(command);
        return Ok(ApiResponse<AuthResponse>.Ok(response));
    }

    /// <summary>
    /// Refresh JWT tokens using a valid refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(RefreshTokenCommand command)
    {
        var response = await _mediator.Send(command);
        return Ok(ApiResponse<AuthResponse>.Ok(response));
    }
}