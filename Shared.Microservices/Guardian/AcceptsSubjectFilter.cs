using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.DataAccess.Providers.Interfaces;
using Shared.Microservices.Authorization;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Services;

namespace Shared.Microservices.Guardian;

public sealed class AcceptsSubjectFilter : IAsyncAuthorizationFilter
{
    /// <summary>
    /// The constant-time guard against enumerating who is a minor: an acting-as request costs the
    /// same whether or not the pair exists. Unconditional, before any branch that could reveal it,
    /// and deliberately not configurable.
    /// </summary>
    public const int DelayMilliseconds = 100;

    private readonly GuardianPermission _permission;
    private readonly ConsentType? _consent;
    private readonly IGuardianAuthorizer _authorizer;
    private readonly IJwtPayloadProvider _jwtPayloadProvider;
    private readonly TimeProvider _timeProvider;

    public AcceptsSubjectFilter(
        GuardianPermission permission,
        ConsentType? consent,
        IGuardianAuthorizer authorizer,
        IJwtPayloadProvider jwtPayloadProvider,
        TimeProvider? timeProvider = null)
    {
        _permission = permission;
        _consent = consent;
        _authorizer = authorizer;
        _jwtPayloadProvider = jwtPayloadProvider;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        var jwtUserId = CallerIdentity.UserId(httpContext, _jwtPayloadProvider);

        if (!httpContext.Request.Headers.TryGetValue(GuardianContextKeys.ActingAsHeader, out var rawValue))
        {
            BindTheCallerAsItsOwnSubject(httpContext, jwtUserId);
            return;
        }

        if (jwtUserId is not { } actorUserId)
            throw new UnauthorizedException("Authentication required", ErrorCodeEnum.Unauthorized);

        if (!Guid.TryParse(rawValue.ToString(), out var subjectUserId))
            throw new BadRequestException("X-Acting-As must be a valid GUID",
                ErrorCodeEnum.ActingAsValidationFailed);

        if (subjectUserId == actorUserId)
        {
            /*
             * Naming yourself asks for nothing the request without the header does not, so it is
             * answered as one. It was a 400 until installed app builds turned out to send the
             * caller's own id when the caller reads their own RSVP. No delay either: it hides
             * whether two people are linked, and a caller's own id tells them nothing.
             */
            BindTheCallerAsItsOwnSubject(httpContext, actorUserId);
            return;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(DelayMilliseconds), _timeProvider,
            httpContext.RequestAborted);

        var authorization = await _authorizer.AuthorizeAsync(actorUserId, subjectUserId, _permission,
            _consent, httpContext.Request, httpContext.RequestAborted);

        httpContext.Items[GuardianContextKeys.SubjectUserId] = subjectUserId;
        httpContext.Items[GuardianContextKeys.ActorUserId] = actorUserId;
        httpContext.Items[GuardianContextKeys.AuthorizationSource] = authorization.AuthorizationSource;
        httpContext.Items[GuardianContextKeys.Processed] = true;
    }

    /// <summary>
    /// The self-serve caller is its own subject. Guid.Empty for an anonymous caller is what an
    /// [AllowAnonymous] action has always read out of an absent JWT, so marking an endpoint changes
    /// nothing for everyone who sends no header.
    /// </summary>
    private static void BindTheCallerAsItsOwnSubject(HttpContext httpContext, Guid? jwtUserId)
    {
        httpContext.Items[GuardianContextKeys.SubjectUserId] = jwtUserId ?? Guid.Empty;
        httpContext.Items[GuardianContextKeys.ActorUserId] = jwtUserId ?? Guid.Empty;
    }
}
