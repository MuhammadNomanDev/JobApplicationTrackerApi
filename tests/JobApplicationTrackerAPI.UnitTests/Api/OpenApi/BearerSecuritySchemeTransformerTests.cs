using AwesomeAssertions;
using JobApplicationTrackerAPI.Api.OpenApi;
using Microsoft.OpenApi;

namespace JobApplicationTrackerAPI.UnitTests.Api.OpenApi;

/// <summary>
/// P1/M1j: the Bearer security-scheme transformer adds the JWT scheme and a
/// global security requirement to the generated OpenAPI document.
/// The transformer ignores the context, so it is passed as null.
/// </summary>
public class BearerSecuritySchemeTransformerTests
{
    [Fact]
    public async Task TransformAsync_AddsBearerScheme_AndSecurityRequirement()
    {
        var document = new OpenApiDocument();
        var transformer = new BearerSecuritySchemeTransformer();

        await transformer.TransformAsync(document, null!, CancellationToken.None);

        document.Components.Should().NotBeNull();
        var scheme = document.Components!.SecuritySchemes!["Bearer"]
            .Should().BeOfType<OpenApiSecurityScheme>().Subject;
        scheme.Type.Should().Be(SecuritySchemeType.Http);
        scheme.Scheme.Should().Be("Bearer");
        scheme.BearerFormat.Should().Be("JWT");
        scheme.In.Should().Be(ParameterLocation.Header);
        document.Security.Should().HaveCount(1);
    }

    [Fact]
    public async Task TransformAsync_ReusesExistingComponents()
    {
        var components = new OpenApiComponents();
        var document = new OpenApiDocument { Components = components };
        var transformer = new BearerSecuritySchemeTransformer();

        await transformer.TransformAsync(document, null!, CancellationToken.None);

        document.Components.Should().BeSameAs(components);
        document.Components.SecuritySchemes.Should().ContainKey("Bearer");
    }
}
