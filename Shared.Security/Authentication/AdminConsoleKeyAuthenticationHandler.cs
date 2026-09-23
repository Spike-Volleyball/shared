using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Microservices.Authorization;

namespace Shared.Security.Authentication;

/// <summary>
/// Authenticates the admin console by its shared secret, as a scheme only the
/// <see cref="Authorization.SpikePolicies.AdminConsole"/> policy asks for. A user token never
/// satisfies that policy, and this key never satisfies the user fallback.
/// </summary>
public sealed class AdminConsoleKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AdminConsoleSettings> settings)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "AdminConsoleKey";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AdminConsoleKey.HeaderName, out var presented))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!AdminConsoleKey.Matches(settings.Value, presented.ToString()))
            return Task.FromResult(AuthenticateResult.Fail("The admin console key did not match."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin-console")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
