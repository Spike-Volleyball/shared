using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Security.PublicReads;

public static class PublicReadLimitExtensions
{
    /// <summary>
    /// Registers <see cref="PublicReadLimit"/> under its name, from the <see cref="PublicReadSettings.SectionName"/>
    /// section. The pipeline runs it with <c>UseRateLimiter()</c>, after <c>UseAuthentication()</c>: whoever is
    /// signed in is not counted, so the limit must know who that is.
    /// </summary>
    public static IServiceCollection AddPublicReadLimit(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PublicReadSettings>(configuration.GetSection(PublicReadSettings.SectionName));
        services.AddRateLimiter(options => options.AddPolicy<string, PublicReadLimit>(PublicReadLimit.PolicyName));
        return services;
    }
}
