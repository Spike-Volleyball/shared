using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shared.DataAccess.Providers;
using Shared.DataAccess.Providers.Interfaces;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Microservices.Guardian;
using Shared.Services;

namespace Shared.Tests.Microservices;

[TestFixture]
[Category("Unit")]
public class AcceptsSubjectFilterTests
{
    private const string AuthSource = "cache";

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();

    private FakeTimeProvider _timeProvider = null!;
    private IGuardianAuthorizer _authorizer = null!;
    private AcceptsSubjectFilter _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _timeProvider = new FakeTimeProvider();
        _authorizer = Substitute.For<IGuardianAuthorizer>();
        _authorizer.AuthorizeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<GuardianPermission>(),
                Arg.Any<ConsentType?>(), Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GuardianAuthorization(GuardianPermission.Register, AuthSource));

        _sut = FilterFor();
    }

    private AcceptsSubjectFilter FilterFor(
        GuardianPermission permission = GuardianPermission.Register,
        ConsentType? consent = ConsentType.EventParticipation) =>
        new(permission, consent, _authorizer, new JwtPayloadProvider(), _timeProvider);

    private static AuthorizationFilterContext Context(Guid? jwtUserId, string? actingAs = null)
    {
        var httpContext = new DefaultHttpContext();

        httpContext.User = jwtUserId is { } id
            ? new ClaimsPrincipal(new ClaimsIdentity([new Claim("nameid", id.ToString())], "TestAuth"))
            : new ClaimsPrincipal(new ClaimsIdentity());

        if (actingAs is not null)
            httpContext.Request.Headers[GuardianContextKeys.ActingAsHeader] = actingAs;

        return new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            []);
    }

    private Task ReleaseTheDelayAndAwait(Task authorization)
    {
        _timeProvider.Advance(TimeSpan.FromMilliseconds(AcceptsSubjectFilter.DelayMilliseconds));
        return authorization;
    }

    [Test]
    public async Task NoHeader_BindsTheJwtUserAsSubject()
    {
        // Arrange
        var context = Context(ActorId);

        // Act
        await _sut.OnAuthorizationAsync(context);

        // Assert
        context.HttpContext.Items[GuardianContextKeys.SubjectUserId].Should().Be(ActorId);
        context.HttpContext.Items[GuardianContextKeys.ActorUserId].Should().Be(ActorId);
    }

    [Test]
    public async Task NoHeader_DoesNotCallTheAuthorizer()
    {
        // Arrange
        var context = Context(ActorId);

        // Act
        await _sut.OnAuthorizationAsync(context);

        // Assert
        await _authorizer.DidNotReceiveWithAnyArgs()
            .AuthorizeAsync(default, default, default, default, null!, default);
    }

    /// <summary>
    /// An [AllowAnonymous] action reads Guid.Empty out of an absent JWT; the bound subject has to
    /// say the same thing rather than fail the request.
    /// </summary>
    [Test]
    public async Task NoHeaderAndAnonymous_BindsTheEmptyGuid()
    {
        // Arrange
        var context = Context(jwtUserId: null);

        // Act
        await _sut.OnAuthorizationAsync(context);

        // Assert
        context.HttpContext.Items[GuardianContextKeys.SubjectUserId].Should().Be(Guid.Empty);
    }

    [Test]
    public async Task HeaderPresentAndUnauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        var context = Context(jwtUserId: null, actingAs: SubjectId.ToString());

        // Act & Assert
        (await _sut.Invoking(f => f.OnAuthorizationAsync(context))
                .Should().ThrowAsync<UnauthorizedException>())
            .Which.ErrorCode.Should().Be(ErrorCodeEnum.Unauthorized);
    }

    [Test]
    public async Task HeaderNotAGuid_ThrowsBadRequestActingAsValidationFailed()
    {
        // Arrange
        var context = Context(ActorId, actingAs: "not-a-guid");

        // Act & Assert
        (await _sut.Invoking(f => f.OnAuthorizationAsync(context))
                .Should().ThrowAsync<BadRequestException>())
            .Which.Should().Match<BadRequestException>(e =>
                e.Message == "X-Acting-As must be a valid GUID" &&
                e.ErrorCode == ErrorCodeEnum.ActingAsValidationFailed);
    }

    /// <summary>
    /// Naming yourself asks for what the request without the header asks for. Installed apps send
    /// their own id when the caller reads their own RSVP, and refusing it failed that screen.
    /// </summary>
    [Test]
    public async Task HeaderEqualsTheJwtUser_BindsTheJwtUserAsSubjectAsNoHeaderDoes()
    {
        // Arrange
        var context = Context(ActorId, actingAs: ActorId.ToString());

        // Act
        await _sut.OnAuthorizationAsync(context);

        // Assert
        context.HttpContext.Items[GuardianContextKeys.SubjectUserId].Should().Be(ActorId);
        context.HttpContext.Items[GuardianContextKeys.ActorUserId].Should().Be(ActorId);
        context.HttpContext.Items.ContainsKey(GuardianContextKeys.Processed).Should().BeFalse();
        context.HttpContext.Items.ContainsKey(GuardianContextKeys.AuthorizationSource).Should().BeFalse();
    }

    /// <summary>
    /// The delay hides whether two people are linked. A caller naming themselves reveals nothing
    /// by being answered at once.
    /// </summary>
    [Test]
    public async Task HeaderEqualsTheJwtUser_NeitherWaitsNorCallsTheAuthorizer()
    {
        // Arrange
        var context = Context(ActorId, actingAs: ActorId.ToString());

        // Act
        var authorization = _sut.OnAuthorizationAsync(context);

        // Assert
        authorization.IsCompletedSuccessfully.Should().BeTrue();
        await _authorizer.DidNotReceiveWithAnyArgs()
            .AuthorizeAsync(default, default, default, default, null!, default);
    }

    [Test]
    public async Task HeaderPresent_DelaysBeforeAnyAccessCheck()
    {
        // Arrange
        var context = Context(ActorId, actingAs: SubjectId.ToString());

        // Act
        var authorization = _sut.OnAuthorizationAsync(context);

        // Assert
        authorization.IsCompleted.Should().BeFalse();
        await _authorizer.DidNotReceiveWithAnyArgs()
            .AuthorizeAsync(default, default, default, default, null!, default);

        await ReleaseTheDelayAndAwait(authorization);

        await _authorizer.ReceivedWithAnyArgs(1)
            .AuthorizeAsync(default, default, default, default, null!, default);
    }

    /// <summary>
    /// The delay is the constant-time guard against enumerating who is a minor. Pinned by the
    /// boundary rather than by reading the constant back, so shortening it fails here.
    /// </summary>
    [Test]
    public async Task HeaderPresent_DelaysForOneHundredMilliseconds()
    {
        // Arrange
        var context = Context(ActorId, actingAs: SubjectId.ToString());

        // Act
        var authorization = _sut.OnAuthorizationAsync(context);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(99));

        // Assert
        authorization.IsCompleted.Should().BeFalse();

        _timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        await authorization;
    }

    [Test]
    public async Task HeaderPresent_PassesTheDeclaredPermissionAndConsentToTheAuthorizer()
    {
        // Arrange
        var context = Context(ActorId, actingAs: SubjectId.ToString());
        _sut = FilterFor(GuardianPermission.Pay, ConsentType.PaymentProcessing);

        // Act
        await ReleaseTheDelayAndAwait(_sut.OnAuthorizationAsync(context));

        // Assert
        await _authorizer.Received(1).AuthorizeAsync(ActorId, SubjectId, GuardianPermission.Pay,
            ConsentType.PaymentProcessing, context.HttpContext.Request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AuthorizerThrows_ExceptionPropagatesUnwrapped()
    {
        // Arrange
        var context = Context(ActorId, actingAs: SubjectId.ToString());
        _authorizer.AuthorizeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<GuardianPermission>(),
                Arg.Any<ConsentType?>(), Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>())
            .Returns<GuardianAuthorization>(_ => throw new ForbiddenException("Access denied"));

        // Act & Assert
        await _sut.Invoking(f => ReleaseTheDelayAndAwait(f.OnAuthorizationAsync(context)))
            .Should().ThrowAsync<ForbiddenException>();
        context.Result.Should().BeNull();
    }

    [Test]
    public async Task Success_SetsSubjectActorSourceAndProcessed()
    {
        // Arrange
        var context = Context(ActorId, actingAs: SubjectId.ToString());

        // Act
        await ReleaseTheDelayAndAwait(_sut.OnAuthorizationAsync(context));

        // Assert
        context.HttpContext.Items[GuardianContextKeys.SubjectUserId].Should().Be(SubjectId);
        context.HttpContext.Items[GuardianContextKeys.ActorUserId].Should().Be(ActorId);
        context.HttpContext.Items[GuardianContextKeys.AuthorizationSource].Should().Be(AuthSource);
        context.HttpContext.Items[GuardianContextKeys.Processed].Should().Be(true);
    }

    /// <summary>
    /// ConsentType.None is the attribute's sentinel for "no consent gate", never a grantable
    /// consent — it must not reach the authorizer as one.
    /// </summary>
    [Test]
    public void AttributeWithNoConsent_CarriesANullConsent()
    {
        // Arrange
        // Act
        var attribute = new AcceptsSubjectAttribute(GuardianPermission.Register, ConsentType.None);

        // Assert
        attribute.Consent.Should().BeNull();
        attribute.Permission.Should().Be(GuardianPermission.Register);
    }

    [Test]
    public void AttributeWithNoArguments_DeclaresNeitherPermissionNorConsent()
    {
        // Arrange
        // Act
        var attribute = new AcceptsSubjectAttribute();

        // Assert
        attribute.Permission.Should().Be(GuardianPermission.None);
        attribute.Consent.Should().BeNull();
    }

    /// <summary>
    /// The reason the attribute builds its own filter: TypeFilterAttribute passes Arguments by
    /// their boxed runtime types, which cannot express a ConsentType? — a present consent boxes
    /// as ConsentType and matches no ConsentType? parameter, and an absent one is a null the
    /// argument-type scan dereferences. Both cases are covered here.
    /// </summary>
    [TestCase(ConsentType.PaymentProcessing)]
    [TestCase(ConsentType.None)]
    public void Attribute_CreatesTheFilterFromTheServiceProvider(ConsentType consent)
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton(_authorizer)
            .AddSingleton<IJwtPayloadProvider, JwtPayloadProvider>()
            .BuildServiceProvider();

        // Act
        var filter = new AcceptsSubjectAttribute(GuardianPermission.Pay, consent).CreateInstance(services);

        // Assert
        filter.Should().BeOfType<AcceptsSubjectFilter>();
    }

    /// <summary>
    /// A controller-level [AcceptsSubject] would silently mark every action on it, admin ones
    /// included — the exact class of accident this attribute exists to remove.
    /// </summary>
    [Test]
    public void Attribute_IsMethodOnly()
    {
        // Arrange
        // Act
        var usage = typeof(AcceptsSubjectAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.ValidOn.Should().Be(AttributeTargets.Method);
        usage.AllowMultiple.Should().BeFalse();
    }
}
