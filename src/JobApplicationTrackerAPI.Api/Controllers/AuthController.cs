using JobApplicationTracker.Application.Features.Auth.Commands;
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

    [HttpPost("register")] // POST /api/auth/register
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var response = await _mediator.Send(command);
        return Ok(new { token = response.Token, refreshToken = response.RefreshToken, expiration = response.Expiration });
    }

    [HttpPost("login")] // POST /api/auth/login
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var response = await _mediator.Send(new LoginCommand(command.Email, command.Password));
        return Ok(new { token = response.Token, refreshToken = response.RefreshToken, expiration = response.Expiration });
    }

    [HttpPost("refresh")] // POST /api/auth/refresh
    public async Task<IActionResult> RefreshToken(RefreshTokenCommand command)
    {
        var response = await _mediator.Send(command);
        return Ok(new { token = response.Token, refreshToken = response.RefreshToken, expiration = response.Expiration });
    }
}