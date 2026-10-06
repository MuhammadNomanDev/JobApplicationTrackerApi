using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace JobApplicationTrackerAPI.Api.OpenApi;

/// <summary>
/// Adds the JWT Bearer security scheme to the generated OpenAPI document.
/// This replaces the Swashbuckle <c>AddSecurityDefinition</c>/<c>AddSecurityRequirement</c>
/// configuration that was removed in P1/M1c, so the Scalar UI keeps its "Authorize" support.
///
/// Written against the Microsoft.OpenApi v2 model (flattened <c>Microsoft.OpenApi</c>
/// namespace; <c>OpenApiSecuritySchemeReference</c> instead of the v1
/// <c>OpenApiReference</c> + <c>ReferenceType.SecurityScheme</c> pattern).
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var components = document.Components ??= new OpenApiComponents();
        var schemes = components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        schemes["Bearer"] = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT bearer token.\n\nExample: \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
        };

        var security = document.Security ??= [];
        security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer", document),
                []
            }
        });

        return Task.CompletedTask;
    }
}
