using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.DataAccess.Providers;
using Shared.DataAccess.Providers.Interfaces;
using Shared.Middleware;
using Shared.Security.Authentication;
using Shared.Security.Authorization;

namespace Shared.Tests.Security;

/// <summary>
/// A minimal service wired the way an adopting service is: both registrations, the shared error
/// middleware and MVC over this assembly's controllers, then whatever else a test maps.
/// </summary>
internal sealed class SecuredTestApp : IAsyncDisposable
{
    public const string ConsoleKey = "the-console-secret";

    private const string Secret = "test-secret-key-for-security-tests-minimum-32-characters";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";

    private readonly WebApplication _app;

    private SecuredTestApp(WebApplication app)
    {
        _app = app;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }

    public IServiceProvider Services => _app.Services;

    public static async Task<SecuredTestApp> StartAsync(
        Action<WebApplication> map, Action<IServiceCollection>? services = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = Secret,
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["AdminConsole:ApiKey"] = ConsoleKey,
        });
        builder.Services.AddSpikeAuthentication(builder.Configuration);
        builder.Services.AddSpikeAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(SecuredTestApp).Assembly);
        builder.Services.AddScoped<IJwtPayloadProvider, JwtPayloadProvider>();
        services?.Invoke(builder.Services);

        var app = builder.Build();
        app.UseMiddleware<ErrorHandlerMiddleware>();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        map(app);
        app.MapControllers();

        await app.StartAsync();
        return new SecuredTestApp(app);
    }

    public static string UserToken(Guid userId)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Secret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
