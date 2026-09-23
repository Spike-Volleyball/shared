using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shared.DataAccess.Providers;
using Shared.DataAccess.Providers.Interfaces;
using Shared.Security.Access;
using Shared.Security.Hubs;

namespace Shared.Tests.Security;

[TestFixture]
[Category("Unit")]
public class SecureHubTests
{
    private const string Room = "probe-room";

    private sealed class ProbeHub(IServiceProvider services) : SecureHub(services)
    {
        public Task Join(Guid probeId) => JoinResourceGroupAsync(Room, probeId, ProbeAccess.Read);

        public Task Act(Guid probeId) => EnsureAccessAsync(probeId, ProbeAccess.Read);
    }

    private sealed class Caller(ClaimsPrincipal user) : HubCallerContext
    {
        public override string ConnectionId => "connection-1";
        public override string? UserIdentifier => null;
        public override ClaimsPrincipal? User => user;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() { }
    }

    private IGroupManager _groups = null!;

    [SetUp]
    public void SetUp() => _groups = Substitute.For<IGroupManager>();

    [Test]
    public async Task JoinResourceGroup_ForAMember_JoinsTheRoom()
    {
        // Act
        await HubFor(ProbeAuthority.Member).Join(ProbeAuthority.PrivateProbe);

        // Assert
        await _groups.Received(1).AddToGroupAsync("connection-1", Room, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task JoinResourceGroup_ForAStranger_IsRefusedAndJoinsNothing()
    {
        // Act
        var act = () => HubFor(Guid.NewGuid()).Join(ProbeAuthority.PrivateProbe);

        // Assert
        await act.Should().ThrowAsync<HubException>();
        await _groups.DidNotReceiveWithAnyArgs().AddToGroupAsync(default!, default!, default);
    }

    [Test]
    public async Task JoinResourceGroup_ForASignedOutCaller_IsRefused()
    {
        // Act
        var act = () => HubFor(null).Join(ProbeAuthority.PrivateProbe);

        // Assert
        await act.Should().ThrowAsync<HubException>();
    }

    [Test]
    public async Task EnsureAccess_ForAMember_LetsTheMethodGoOn()
    {
        // Act
        var act = () => HubFor(ProbeAuthority.Member).Act(ProbeAuthority.PrivateProbe);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task EnsureAccess_ForAStranger_RefusesAsForAMissingResource()
    {
        // Arrange — a hub method acting on a resource by id asks what a route with [Access] asks.
        var hub = HubFor(Guid.NewGuid());

        // Act
        var hidden = () => hub.Act(ProbeAuthority.PrivateProbe);
        var missing = () => hub.Act(Guid.NewGuid());

        // Assert
        (await hidden.Should().ThrowAsync<HubException>()).Which.Message
            .Should().Be((await missing.Should().ThrowAsync<HubException>()).Which.Message);
    }

    private ProbeHub HubFor(Guid? userId)
    {
        var services = new ServiceCollection()
            .AddScoped<IResourceAuthority<ProbeAccess>, ProbeAuthority>()
            .AddScoped<IJwtPayloadProvider, JwtPayloadProvider>()
            .BuildServiceProvider();
        var identity = userId is { } id
            ? new ClaimsIdentity([new Claim("sub", id.ToString())], "Bearer")
            : new ClaimsIdentity();

        return new ProbeHub(services) { Context = new Caller(new ClaimsPrincipal(identity)), Groups = _groups };
    }
}
