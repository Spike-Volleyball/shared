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
            /*
             * The self-serve caller is its own subject. Guid.Empty for an anonymous caller is what
             * an [AllowAnonymous] action has always read out of an absent JWT, so marking an
             * endpoint changes nothing for everyone who sends no header.
             */
            httpContext.Items[GuardianContextKeys.SubjectUserId] = jwtUserId ?? Guid.Empty;
            httpContext.Items[GuardianContextKeys.ActorUserId] = jwtUserId ?? Guid.Empty;
            return;
        }

        if (jwtUserId is not { } actorUserId)
            throw new UnauthorizedException("Authentication required", ErrorCodeEnum.Unauthorized);

        if (!Guid.TryParse(rawValue.ToString(), out var subjectUserId))
            throw new BadRequestException("X-Acting-As must be a valid GUID",
                ErrorCodeEnum.ActingAsValidationFailed);

        if (subjectUserId == actorUserId)
            throw new BadRequestException("Cannot act as yourself",
                ErrorCodeEnum.ActingAsValidationFailed);

        await Task.Delay(TimeSpan.FromMilliseconds(DelayMilliseconds), _timeProvider,
            httpContext.RequestAborted);

        var authorization = await _authorizer.AuthorizeAsync(actorUserId, subjectUserId, _permission,
            _consent, httpContext.Request, httpContext.RequestAborted);

        httpContext.Items[GuardianContextKeys.SubjectUserId] = subjectUserId;
        httpContext.Items[GuardianContextKeys.ActorUserId] = actorUserId;
        httpContext.Items[GuardianContextKeys.AuthorizationSource] = authorization.AuthorizationSource;
        httpContext.Items[GuardianContextKeys.Processed] = true;
    }
}
