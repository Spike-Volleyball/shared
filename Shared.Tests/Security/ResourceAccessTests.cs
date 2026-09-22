using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shared.Security.Access;
using Shared.Security.Authorization;
using Shared.Services;

namespace Shared.Tests.Security;

public enum ProbeAccess
{
    Read,
}

/// <summary>A resource only its member may read, beside one anyone may.</summary>
public sealed class ProbeAuthority : IResourceAuthority<ProbeAccess>
{
    public static readonly Guid PublicProbe = Guid.NewGuid();
    public static readonly Guid PrivateProbe = Guid.NewGuid();
    public static readonly Guid Member = Guid.NewGuid();

    public Task<bool> CanAsync(Guid? userId, Guid resourceId, ProbeAccess access, CancellationToken ct) =>
        Task.FromResult(resourceId == PublicProbe || (resourceId == PrivateProbe && userId == Member));
}

[ApiController]
[Route("probes")]
public sealed class ProbesController : ControllerBase
{
    [HttpGet("{probeId:guid}")]
    [PublicEndpoint("A probe some of which anyone may read")]
    [Access<ProbeAccess>(ProbeAccess.Read, "probeId")]
    public string Get(Guid probeId) => $"probe {probeId}";
}

[TestFixture]
[Category("Unit")]
public class ResourceAccessTests
{
    /// <summary>Stands in for AcceptsSubjectFilter, which records a validated subject the same way.</summary>
    private const string TestSubjectHeader = "X-Test-Subject";

    private SecuredTestApp _app = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _app = await SecuredTestApp.StartAsync(
            app => app.Use(async (context, next) =>
            {
                if (Guid.TryParse(context.Request.Headers[TestSubjectHeader], out var subject))
                    context.Items[GuardianContextKeys.SubjectUserId] = subject;
                await next(context);
            }),
            services => services.AddScoped<IResourceAuthority<ProbeAccess>, ProbeAuthority>());
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _app.DisposeAsync();

    [Test]
    public async Task AResourceTheAuthorityOpensToAll_IsReadAnonymously()
    {
        // Act
        var response = await GetAsync(ProbeAuthority.PublicProbe);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task APrivateResource_IsReadByItsMember()
    {
        // Act
        var response = await GetAsync(ProbeAuthority.PrivateProbe, ProbeAuthority.Member);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task APrivateResource_RefusesAStrangerExactlyAsAMissingOne()
    {
        // Arrange
        var stranger = Guid.NewGuid();

        // Act
        var refused = await GetAsync(ProbeAuthority.PrivateProbe, stranger);
        var missing = await GetAsync(Guid.NewGuid(), stranger);

        // Assert
        refused.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await refused.Content.ReadAsStringAsync()).Should().Be(await missing.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task APrivateResource_AsksASignedOutCallerToSignIn_AsAMissingOneDoes()
    {
        // Act
        var refused = await GetAsync(ProbeAuthority.PrivateProbe);
        var missing = await GetAsync(Guid.NewGuid());

        // Assert
        refused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        missing.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await refused.Content.ReadAsStringAsync()).Should().Be(await missing.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task AGuardianActingForAMember_IsJudgedAsTheMember()
    {
        // Arrange
        var guardian = Guid.NewGuid();

        // Act
        var response = await GetAsync(ProbeAuthority.PrivateProbe, guardian, subject: ProbeAuthority.Member);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public void NoResourceScope_WithoutAReason_CannotBeDeclared()
    {
        // Act
        var act = () => new NoResourceScopeAttribute(" ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private async Task<HttpResponseMessage> GetAsync(Guid probeId, Guid? userId = null, Guid? subject = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/probes/{probeId}");
        if (userId is { } id)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SecuredTestApp.UserToken(id));
        if (subject is { } s)
            request.Headers.Add(TestSubjectHeader, s.ToString());

        return await _app.Client.SendAsync(request);
    }
}
