using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using FluentValidation;
using FluentValidation.Results;
using JobApplicationTrackerAPI.Api.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Api.Middleware;

/// <summary>
/// P1/M1j: exception-to-ProblemDetails mapping tests. Each case drives
/// InvokeAsync with a throwing RequestDelegate and reads the written
/// application/problem+json body.
/// </summary>
public class GlobalExceptionHandlerTests
{
    private static (GlobalExceptionHandler Handler, DefaultHttpContext Context) Create(
        Func<HttpContext, Task>? next = null,
        string environmentName = "Production")
    {
        RequestDelegate requestDelegate = next != null
            ? ctx => next(ctx)
            : _ => Task.CompletedTask;
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(environmentName);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(
            requestDelegate, Mock.Of<ILogger<GlobalExceptionHandler>>(), env.Object);
        return (handler, context);
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_Returns400WithErrors()
    {
        var (handler, context) = Create(_ => throw new ValidationException(
            [new ValidationFailure("Email", "Invalid email address")]));

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().Be("application/problem+json");
        var problem = await ReadProblemAsync(context);
        problem.GetProperty("title").GetString().Should().Be("Validation Error");
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("errors").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
    {
        var (handler, context) = Create(_ => throw new UnauthorizedAccessException());

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        var problem = await ReadProblemAsync(context);
        problem.GetProperty("title").GetString().Should().Be("Unauthorized");
        problem.GetProperty("type").GetString().Should().Be("https://httpstatuses.com/401");
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404WithMessage()
    {
        var (handler, context) = Create(_ => throw new KeyNotFoundException("Item 123 not found."));

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        var problem = await ReadProblemAsync(context);
        problem.GetProperty("title").GetString().Should().Be("Not Found");
        problem.GetProperty("detail").GetString().Should().Be("Item 123 not found.");
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_InProduction_HidesDetail()
    {
        var (handler, context) = Create(_ => throw new InvalidOperationException("db exploded"));

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var problem = await ReadProblemAsync(context);
        problem.GetProperty("detail").GetString().Should().Be("An internal server error occurred.");
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_InDevelopment_ShowsDetail()
    {
        var (handler, context) = Create(
            _ => throw new InvalidOperationException("db exploded"),
            environmentName: "Development");

        await handler.InvokeAsync(context);

        var problem = await ReadProblemAsync(context);
        problem.GetProperty("detail").GetString().Should().Be("db exploded");
    }

    [Fact]
    public async Task InvokeAsync_NoException_InvokesNext()
    {
        var nextRan = false;
        var (handler, context) = Create(ctx =>
        {
            nextRan = true;
            return Task.CompletedTask;
        });

        await handler.InvokeAsync(context);

        nextRan.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }
}
