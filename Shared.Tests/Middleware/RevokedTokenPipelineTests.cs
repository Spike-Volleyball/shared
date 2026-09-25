using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Errors;
using Shared.Enums;
using Shared.Middleware;
using Shared.Models.Jwt;
using Shared.Security.Endpoints;
using Shared.Tests.Security;

namespace Shared.Tests.Middleware;

/// <summary>
/// A token minted before its account's sessions ended, through a service's real pipeline: it is
/// no identity at all, so an endpoint open to anyone answers it as signed out and one that needs
/// signing in refuses it.
/// </summary>
[TestFixture]
[Category("Unit")]
public class RevokedTokenPipelineTests
{
    private SecuredTestApp _app = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _app = await SecuredTestApp.StartAsync(
            app =>
            {
                app.UseMiddleware<JwtBlacklistMiddleware>();
                app.MapGet("/probe", (HttpContext context) => context.User.Identity?.IsAuthenticated == true ? "in" : "out")
                    .AllowPublic("Reports whether the caller was authenticated");
                app.MapPost("/sign-in", () => "signed in")
                    .AllowPublic("Signing in: the caller has no token yet");
                app.MapGet("/signed-in-only", () => "ok");
            },
            services => services.AddDistributedMemoryCache());
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _app.DisposeAsync();

    [Test]
    public async Task AStaleTokenOnAPublicRead_IsAnsweredAsSignedOut()
    {
        // Arrange
        var userId = await UserWhoseSessionsEndedAsync();

        // Act
        var response = await SendAsync(HttpMethod.Get, "/probe", userId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("out");
    }

    [Test]
    public async Task AStaleTokenAttachedToSigningIn_DoesNotStandInItsWay()
    {
        // Arrange — the phone that changed the password signs in again with the old token attached
        var userId = await UserWhoseSessionsEndedAsync();

        // Act
        var response = await SendAsync(HttpMethod.Post, "/sign-in", userId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task AStaleTokenWhereSigningInIsRequired_IsRefusedAsAnInvalidToken()
    {
        // Arrange
        var userId = await UserWhoseSessionsEndedAsync();

        // Act
        var response = await SendAsync(HttpMethod.Get, "/signed-in-only", userId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>())!.Code
            .Should().Be(ErrorCodeEnum.TokenInvalid.ToStringCode());
    }

    [Test]
    public async Task ACurrentTokenOnAPublicRead_IsAnsweredAsSignedIn()
    {
        // Act
        var response = await SendAsync(HttpMethod.Get, "/probe", Guid.NewGuid());

        // Assert
        (await response.Content.ReadAsStringAsync()).Should().Be("in");
    }

    /// <summary>
    /// An account whose session version auth has moved past the first, so a token minted for it
    /// without the claim, as <see cref="SecuredTestApp.UserToken" /> mints them, is stale.
    /// </summary>
    private async Task<Guid> UserWhoseSessionsEndedAsync()
    {
        var userId = Guid.NewGuid();
        await _app.Services.GetRequiredService<IDistributedCache>()
            .SetStringAsync(SessionVersions.CacheKey(userId.ToString()), (SessionVersions.Initial + 1).ToString());
        return userId;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Guid userId)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SecuredTestApp.UserToken(userId));
        return await _app.Client.SendAsync(request);
    }
}
