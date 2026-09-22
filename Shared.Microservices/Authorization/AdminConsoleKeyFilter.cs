using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Shared.Microservices.Authorization;

/// <summary>
/// Refuses any request that does not carry the admin console's shared secret.
/// </summary>
/// <remarks>
/// One implementation for every service on purpose; the comparison itself lives in
/// <see cref="AdminConsoleKey"/>, which the authentication scheme uses too.
/// </remarks>
public sealed class AdminConsoleKeyFilter : IAuthorizationFilter
{
    public const string HeaderName = AdminConsoleKey.HeaderName;

    private readonly AdminConsoleSettings _settings;

    public AdminConsoleKeyFilter(IOptions<AdminConsoleSettings> settings)
    {
        _settings = settings.Value;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var presented = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (!AdminConsoleKey.Matches(_settings, presented))
        {
            /* No body. A refusal that explains itself tells a prober what to try next. */
            context.Result = new UnauthorizedResult();
        }
    }
}
