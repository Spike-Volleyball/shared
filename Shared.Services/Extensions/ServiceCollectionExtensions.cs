using Amazon.CloudFront;
using Amazon.S3;
using Amazon.SimpleEmailV2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Options;
using Shared.Services.Embeds;
using Shared.Services.FileStorage;
using Shared.Services.FileStorage.Intefaces;
using Shared.Services.Interfaces;
using Shared.Services.Logging;
using Shared.Services.Services;
using Shared.Services.Services.Interfaces;

namespace Shared.Services.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Backward-compatible overload for services that don't send email.
    /// Always registers SES email service (never resolved by these services).
    /// </summary>
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, EmailService>();
        services.AddAWSService<IAmazonSimpleEmailServiceV2>();

        return AddSharedServicesCore(services);
    }

    /// <summary>
    /// Full overload for services that send email. Supports SMTP transport for staging/development.
    /// </summary>
    public static IServiceCollection AddSharedServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var emailSection = configuration.GetSection("EmailSettings");

        if (emailSection.Exists())
        {
            var emailSettings = emailSection.Get<EmailSettings>();
            var useSmtp = emailSettings?.SmtpTransportEnabled == true
                          && !string.IsNullOrWhiteSpace(emailSettings.SmtpHost);

            if (useSmtp)
            {
                services.AddScoped<IEmailService, SmtpEmailService>();
            }
            else
            {
                services.AddScoped<IEmailService, EmailService>();
                services.AddAWSService<IAmazonSimpleEmailServiceV2>();
            }
        }

        return AddSharedServicesCore(services);
    }

    /// <summary>
    /// Video-embed support (URL parsing is static; this wires the oEmbed client).
    /// Opt-in per service — social and messages call it, others don't need it.
    /// </summary>
    public static IServiceCollection AddVideoEmbedServices(this IServiceCollection services)
    {
        services.AddHttpClient<IVideoOEmbedClient, VideoOEmbedClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(2.5);
        });
        return services;
    }

    private static IServiceCollection AddSharedServicesCore(IServiceCollection services)
    {
        services.AddOptions<S3Settings>()
            .BindConfiguration("S3")
            .Validate(s => !string.IsNullOrEmpty(s.PublicBaseUrl) && Uri.TryCreate(s.PublicBaseUrl, UriKind.Absolute, out _),
                "S3:PublicBaseUrl must be a valid absolute URL");

        // Core rather than opt-in: both email transports registered above depend on it.
        services.AddOptions<LogPseudonymizationSettings>()
            .BindConfiguration(LogPseudonymizationSettings.SectionName);
        services.AddSingleton<ILogPseudonymizer, LogPseudonymizer>();
        services.AddHostedService<LogPseudonymizationKeyCheck>();

        services.AddScoped<IHashingService, HashingService>();
        services.AddScoped<IFileService, S3FileService>();
        services.AddScoped<IAgeTierService, AgeTierService>();
        services.AddScoped<ICloudFrontService, CloudFrontService>();
        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonCloudFront>();

        return services;
    }
}
