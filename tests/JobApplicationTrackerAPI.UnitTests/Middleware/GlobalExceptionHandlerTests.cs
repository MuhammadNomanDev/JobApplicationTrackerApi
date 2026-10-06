using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using FluentValidation;
using FluentValidation.Results;
using JobApplicationTrackerAPI.Api.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Middleware;

/// <summary>
/// P1/M1c made the handler's output RFC 9457-clean: the
/// <c>application/problem+json</c> media type and a stable <c>type</c> URI
/// per problem kind.
/// </summary>
public class GlobalExceptionHandlerTests
{
    private static GlobalExceptionHandler CreateHandler(
        RequestDelegate next,
        string? environmentName = null)
    {
        var environment = Mock.Of<IWebHostEnvironment>(e =>
            e.EnvironmentName == (environmentName ?? Environments.Production));
        return new GlobalExceptionHandler(
            next,
            Mock.Of<ILogger<GlobalExceptionHandler>>(),
            environment);
    }

    private static async Task<(HttpContext Context, ProblemDetails? Problem)> InvokeAsync(
        Exception exception,
        string? environmentName = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        var handler = CreateHandler(
            _ => throw exception,
            environmentName);
        await handler.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return (context, problem);
    }

    [Fact]
    public async Task UnauthorizedAccessException_ReturnsProblemJson_WithTypeUri()
    {
        // Act
        var (context, problem) = await InvokeAsync(new UnauthorizedAccessException());

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        context.Response.ContentType.Should().Be("application/problem+json");
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.Unauthorized);
        problem.Type.Should().Be("https://httpstatuses.com/401");
        problem.Title.Should().Be("Unauthorized");
        problem.Instance.Should().Be("/api/test");
    }

    [Fact]
    public async Task ValidationException_ReturnsProblemJson_WithErrorsExtension()
    {
        // Arrange
        var failures = new[]
        {
            new ValidationFailure("Email", "Email is not valid.")
        };
        var exception = new ValidationException(failures);

        // Act
        var (context, problem) = await InvokeAsync(exception);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().Be("application/problem+json");
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Type.Should().Be("https://httpstatuses.com/400");
        problem.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task KeyNotFoundException_ReturnsProblemJson_WithNotFoundType()
    {
        // Act
        var (context, problem) = await InvokeAsync(new KeyNotFoundException("Thing 1 not found."));

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        context.Response.ContentType.Should().Be("application/problem+json");
        problem.Should().NotBeNull();
        problem!.Type.Should().Be("https://httpstatuses.com/404");
        problem.Detail.Should().Be("Thing 1 not found.");
    }

    [Fact]
    public async Task UnhandledException_InProduction_DoesNotLeakDetails()
    {
        // Act
        var (context, problem) = await InvokeAsync(
            new InvalidOperationException("connection string=secret; db exploded"),
            Environments.Production);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        context.Response.ContentType.Should().Be("application/problem+json");
        problem.Should().NotBeNull();
        problem!.Type.Should().Be("https://httpstatuses.com/500");
        problem.Detail.Should().Be("An internal server error occurred.");
        problem.Detail.Should().NotContain("secret");
    }
}
