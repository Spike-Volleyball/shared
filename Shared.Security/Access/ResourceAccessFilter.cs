using Microsoft.AspNetCore.Mvc.Filters;
using Shared.DataAccess.Providers.Interfaces;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Microservices.Authorization;
using Shared.Services;

namespace Shared.Security.Access;

internal sealed class ResourceAccessFilter<TAccess>(
    TAccess access,
    string routeParameter,
    IResourceAuthority<TAccess> authority,
    IJwtPayloadProvider jwtPayloadProvider) : IAsyncAuthorizationFilter
    where TAccess : struct, Enum
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;

        // AcceptsSubjectFilter records the subject only after validating the guardian's standing,
        // so its presence is the proof that asking on the minor's behalf is allowed.
        var userId = httpContext.Items[GuardianContextKeys.SubjectUserId] is Guid subject && subject != Guid.Empty
            ? subject
            : CallerIdentity.UserId(httpContext, jwtPayloadProvider);

        // A route value that is missing or not an id fails closed, like a resource that is not there.
        var allowed = Guid.TryParse(context.RouteData.Values[routeParameter]?.ToString(), out var resourceId)
            && await authority.CanAsync(userId, resourceId, access, httpContext.RequestAborted);

        if (!allowed)
        {
            throw userId is null
                ? new UnauthorizedException("Sign in to see this", ErrorCodeEnum.Unauthorized)
                : new EntityNotFoundException("Not found");
        }
    }
}
