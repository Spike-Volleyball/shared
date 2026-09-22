using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shared.DataAccess.Providers.Interfaces;

namespace Shared.Microservices.Authorization;

public static class CallerIdentity
{
    /// <summary>The user the request's JWT names, or null for a signed-out caller.</summary>
    public static Guid? UserId(HttpContext context, IJwtPayloadProvider jwtPayloadProvider) =>
        UserId(context.User, jwtPayloadProvider, context.Request.Headers.Authorization.ToString());

    /// <summary>The user a principal's claims name - for hubs, whose token never came as a header.</summary>
    public static Guid? UserId(ClaimsPrincipal user, IJwtPayloadProvider jwtPayloadProvider, string accessToken = "")
    {
        if (user.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
            return null;

        var payload = jwtPayloadProvider.GetJwtPayload(identity.Claims, accessToken);
        return payload.UserId == Guid.Empty ? null : payload.UserId;
    }
}
