using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Shared.Microservices.Extensions;

namespace Shared.Tests.Microservices;

[TestFixture]
[Category("Unit")]
public class CorsExtensionsTests
{
    private const string WebApp = "https://app.example";

    [Test]
    public async Task UseFrontendCors_PreflightFromAnAllowedOrigin_MayBeCachedForTwoHours()
    {
        // Arrange
        await using var app = await StartAsync(WebApp);

        // Act
        using var response = await PreflightAsync(app, WebApp);

        // Assert
        response.Headers.GetValues("Access-Control-Max-Age").Should().Equal("7200");
    }

    [Test]
    public async Task UseFrontendCors_PreflightFromAnAllowedOrigin_AllowsCredentialsAndTraceHeaders()
    {
        // Arrange
        await using var app = await StartAsync(WebApp);

        // Act
        using var response = await PreflightAsync(app, WebApp);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal(WebApp);
        response.Headers.GetValues("Access-Control-Allow-Credentials").Should().Equal("true");
        response.Headers.GetValues("Access-Control-Allow-Headers").Single()
            .Should().Contain("traceparent").And.Contain("sentry-trace").And.Contain("baggage");
    }

    [Test]
    public async Task UseFrontendCors_PreflightFromAnotherOrigin_IsNotAllowed()
    {
        // Arrange
        await using var app = await StartAsync(WebApp);

        // Act
        using var response = await PreflightAsync(app, "https://elsewhere.example");

        // Assert
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Test]
    public async Task AddFrontendCors_NoOriginsConfigured_AllowsTheLocalWebApp()
    {
        // Arrange
        await using var app = await StartAsync();

        // Act
        using var response = await PreflightAsync(app, "http://localhost:3000");

        // Assert
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal("http://localhost:3000");
    }

    private static async Task<WebApplication> StartAsync(params string[] allowedOrigins)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        for (var i = 0; i < allowedOrigins.Length; i++)
            builder.Configuration[$"Cors:AllowedOrigins:{i}"] = allowedOrigins[i];
        builder.Services.AddFrontendCors(builder.Configuration);

        var app = builder.Build();
        app.UseFrontendCors();
        app.MapPost("/api/probe", () => "ok");

        await app.StartAsync();
        return app;
    }

    private static Task<HttpResponseMessage> PreflightAsync(WebApplication app, string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/probe");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,traceparent,sentry-trace,baggage");

        return app.GetTestClient().SendAsync(request);
    }
}
