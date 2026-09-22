using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Microservices.Authorization;
using Shared.Security.Authentication;
using Shared.Security.Authorization;
using Shared.Security.Endpoints;

namespace Shared.Tests.Security;

[TestFixture]
[Category("Unit")]
public class DenyByDefaultTests
{
    private const int InternalPort = 5011;

    private SecuredTestApp _app = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _app = await SecuredTestApp.StartAsync(app =>
        {
            app.MapGet("/declares-nothing", () => "ok");
            app.MapGet("/public", () => "ok").AllowPublic("A public page's data");
            app.MapGet("/admin", () => "ok").WithMetadata(new AdminConsoleOnlyAttribute());
            app.MapGet("/hubs/probe", (HttpContext context) => context.User.Identity?.IsAuthenticated == true ? "in" : "out")
                .AllowPublic("Reports whether the caller was authenticated");
            app.MapGet("/probe", (HttpContext context) => context.User.Identity?.IsAuthenticated == true ? "in" : "out")
                .AllowPublic("Reports whether the caller was authenticated");
            app.MapGet("/internal", () => "ok").RequireInternalListener(InternalPort);
        });
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _app.DisposeAsync();

    [Test]
    public async Task AnEndpointThatDeclaresNothing_RefusesAnAnonymousCaller()
    {
        // Act
        var response = await _app.Client.GetAsync("/declares-nothing");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AnEndpointThatDeclaresNothing_AdmitsASignedInUser()
    {
        // Act
        var response = await SendAsync("/declares-nothing", bearer: SecuredTestApp.UserToken(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task APublicEndpoint_AdmitsAnAnonymousCaller()
    {
        // Act
        var response = await _app.Client.GetAsync("/public");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task AnAdminConsoleEndpoint_AdmitsTheConsoleKey()
    {
        // Act
        var response = await SendAsync("/admin", consoleKey: SecuredTestApp.ConsoleKey);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task AnAdminConsoleEndpoint_RefusesAWrongKey()
    {
        // Act
        var response = await SendAsync("/admin", consoleKey: "not-the-console-secret");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AnAdminConsoleEndpoint_RefusesAUserToken()
    {
        // Act
        var response = await SendAsync("/admin", bearer: SecuredTestApp.UserToken(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TheConsoleKey_IsNotASignedInUser()
    {
        // Act
        var response = await SendAsync("/declares-nothing", consoleKey: SecuredTestApp.ConsoleKey);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ATokenInTheQueryString_AuthenticatesAHubRequest()
    {
        // Act
        var body = await _app.Client.GetStringAsync($"/hubs/probe?access_token={SecuredTestApp.UserToken(Guid.NewGuid())}");

        // Assert
        body.Should().Be("in");
    }

    [Test]
    public async Task ATokenInTheQueryString_IsIgnoredOutsideTheHubs()
    {
        // Act
        var body = await _app.Client.GetStringAsync($"/probe?access_token={SecuredTestApp.UserToken(Guid.NewGuid())}");

        // Assert
        body.Should().Be("out");
    }

    [Test]
    public async Task AnInternalEndpoint_IsNotServedOnThePublicPort()
    {
        // Signed in, because a path that matches nothing still meets the fallback: an anonymous
        // caller is answered 401 before routing's 404, which gives away no more.

        // Act
        var response = await SendAsync("http://localhost:5010/internal", bearer: SecuredTestApp.UserToken(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AnInternalEndpoint_IsServedOnTheInternalListenerWithoutAUser()
    {
        // Act
        var response = await _app.Client.GetAsync($"http://localhost:{InternalPort}/internal");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public void AddSpikeAuthentication_WithoutJwtConfiguration_FailsAtStartup()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => new ServiceCollection().AddSpikeAuthentication(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void PublicEndpoint_WithoutAReason_CannotBeDeclared(string reason)
    {
        // Act
        var act = () => new PublicEndpointAttribute(reason);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [TestCase("http://0.0.0.0:5011", 5011)]
    [TestCase("http://+:5021/", 5021)]
    public void GetInternalListenerPort_ReadsThePortOfTheGrpcEndpoint(string url, int expected)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Kestrel:Endpoints:Grpc:Url"] = url })
            .Build();

        // Act & Assert
        configuration.GetInternalListenerPort().Should().Be(expected);
    }

    private async Task<HttpResponseMessage> SendAsync(string url, string? bearer = null, string? consoleKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (consoleKey is not null)
            request.Headers.Add(AdminConsoleKey.HeaderName, consoleKey);

        return await _app.Client.SendAsync(request);
    }
}
