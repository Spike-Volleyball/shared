using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shared.Security.Access;
using Shared.Security.Endpoints;
using Shared.Testing.Security;

namespace Shared.Tests.Security;

[ApiController]
[Route("surface")]
public sealed class SurfaceController : ControllerBase
{
    [HttpGet("{probeId:guid}/misnamed")]
    [Access<ProbeAccess>(ProbeAccess.Read, "thingId")]
    public string Misnamed(Guid probeId) => "never reached";
}

[TestFixture]
[Category("Unit")]
public class EndpointSurfaceTests
{
    private SecuredTestApp _app = null!;
    private IServiceProvider _services = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        WebApplication? built = null;
        _app = await SecuredTestApp.StartAsync(app =>
        {
            built = app;
            app.MapGet("/declares-nothing", () => "ok");
            app.MapGet("/public", () => "ok").AllowPublic("A public page's data");
            app.MapGet("/limited", () => "ok").AllowPublic("A public page's data, a visitor at a time").RequireRateLimiting("probe");
            app.MapPost("/form", () => "ok").AllowPublic("A public form");
            app.MapGet("/bare", () => "ok").AllowAnonymous();
            app.MapGet("/internal", () => "ok").RequireInternalListener(5011);
            app.MapGet("/things/{thingId:guid}", (Guid thingId) => "ok");
            app.MapGet("/scoped/{thingId:guid}", (Guid thingId) => "ok")
                .WithMetadata(new NoResourceScopeAttribute("The caller's own thing, checked in the handler"));
        });
        _services = built!.Services;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _app.DisposeAsync();

    [Test]
    public void Public_ListsEachOpenEndpointWithItsReason()
    {
        // Act
        var surface = EndpointSurface.Public(_services);

        // Assert
        surface.Should().Contain("GET /public - A public page's data")
            .And.Contain("GET /bare - [AllowAnonymous] with no reason")
            .And.Contain(line => line.StartsWith("GET /probes/{probeId:guid} - "))
            .And.NotContain(line => line.Contains("/declares-nothing") || line.Contains("/internal"));
    }

    [Test]
    public async Task Public_WithoutAFallback_ListsEverythingThatDeclaresNothing()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.MapGet("/declares-nothing", () => "ok");
        await app.StartAsync();

        // Act
        var surface = EndpointSurface.Public(app.Services);

        // Assert
        surface.Should().Equal("GET /declares-nothing - declares nothing, and there is no fallback policy");
    }

    [Test]
    public void Internal_ListsTheHostRestrictedEndpoints()
    {
        // Act & Assert
        EndpointSurface.Internal(_services).Should().Equal("GET /internal - internal listener :5011");
    }

    [Test]
    public void BareAnonymous_FindsTheEndpointOpenedWithoutAReason()
    {
        // Act & Assert
        EndpointSurface.BareAnonymous(_services).Should().Equal("GET /bare");
    }

    [Test]
    public void UncheckedIdRoutes_FindsAnIdRouteThatDeclaresNoAccess()
    {
        // Act
        var uncheckedRoutes = EndpointSurface.UncheckedIdRoutes(_services);

        // Assert
        uncheckedRoutes.Should().Contain("GET /things/{thingId:guid}")
            .And.NotContain(line => line.Contains("/scoped/") || line.StartsWith("GET /probes/"));
    }

    [Test]
    public void UnlimitedPublicReads_FindsAPublicReadNoRateLimitCovers()
    {
        // Act
        var unlimited = EndpointSurface.UnlimitedPublicReads(_services);

        // Assert
        unlimited.Should().Contain("GET /public")
            .And.NotContain(line => line.Contains("/limited") || line.Contains("/form")
                                    || line.Contains("/declares-nothing") || line.Contains("/internal"));
    }

    [Test]
    public void MisnamedAccess_FindsADeclarationForAParameterTheRouteLacks()
    {
        // Act & Assert
        EndpointSurface.MisnamedAccess(_services)
            .Should().Equal("GET /surface/{probeId:guid}/misnamed - no route parameter 'thingId'");
    }

    [Test]
    public void ApprovedSnapshot_ReportsNewLinesUntilApproved_ThenPasses()
    {
        // Arrange
        var directory = Directory.CreateTempSubdirectory().FullName;
        var testSource = Path.Combine(directory, "SurfaceTests.cs");
        string[] lines = ["GET /public - A public page's data"];

        // Act
        var first = ApprovedSnapshot.Compare(lines, "public-surface", testSource);
        File.Move(Path.Combine(directory, "public-surface.received.txt"), Path.Combine(directory, "public-surface.approved.txt"));
        var second = ApprovedSnapshot.Compare(lines, "public-surface", testSource);
        var third = ApprovedSnapshot.Compare([.. lines, "GET /new - Opened today"], "public-surface", testSource);

        // Assert
        first.Should().Contain("No approved public-surface yet");
        second.Should().BeNull();
        third.Should().Contain("+ GET /new - Opened today");
    }

    [Test]
    public async Task ResponseJson_FindsAKeyAtAnyDepth_ButNotANullOne()
    {
        // Arrange
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(new
            {
                team = new { captain = new { name = "Marta", email = "marta@test.com" } },
                holder = new { dateOfBirth = (string?)null },
            }),
        };

        // Act
        var paths = await ResponseJson.PathsOfAsync(response, ResponseJson.PersonalDataKeys);

        // Assert
        paths.Should().Equal("$.team.captain.email");
    }
}
