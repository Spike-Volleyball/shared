using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Security.PublicReads;

public static class PublicWriteLimitExtensions
{
    /// <summary>
    /// Registers <see cref="PublicWriteLimit"/> under its name, from the <see cref="PublicWriteSettings.SectionName"/>
    /// section. The pipeline runs it with <c>UseRateLimiter()</c>, as it does the read limit.
    /// </summary>
    public static IServiceCollection AddPublicWriteLimit(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PublicWriteSettings>(configuration.GetSection(PublicWriteSettings.SectionName));
        services.AddRateLimiter(options => options.AddPolicy<string, PublicWriteLimit>(PublicWriteLimit.PolicyName));
        return services;
    }
}
