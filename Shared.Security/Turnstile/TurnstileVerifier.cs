using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Enums;
using Shared.Exceptions;

namespace Shared.Security.Turnstile;

public class TurnstileVerifier : ITurnstileVerifier
{
    private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly HttpClient _httpClient;
    private readonly TurnstileSettings _settings;
    private readonly ILogger<TurnstileVerifier> _logger;

    public TurnstileVerifier(
        HttpClient httpClient,
        IOptions<TurnstileSettings> settings,
        ILogger<TurnstileVerifier> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task RequireChallengePassedAsync(string? token, string? clientIp)
    {
        if (string.IsNullOrEmpty(_settings.SecretKey))
            return;

        if (string.IsNullOrWhiteSpace(token))
            throw ChallengeFailed();

        // null = Cloudflare unreachable, which the failure already logged.
        if (await VerifyAsync(token, clientIp) == false)
        {
            _logger.LogWarning("Turnstile verification rejected a request");
            throw ChallengeFailed();
        }
    }

    private async Task<bool?> VerifyAsync(string token, string? clientIp)
    {
        try
        {
            var form = new List<KeyValuePair<string, string>>
            {
                new("secret", _settings.SecretKey!),
                new("response", token),
            };
            if (!string.IsNullOrEmpty(clientIp))
                form.Add(new("remoteip", clientIp));

            var response = await _httpClient.PostAsync(SiteVerifyUrl, new FormUrlEncodedContent(form));
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>();
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Turnstile siteverify call failed — allowing the request through");
            return null;
        }
    }

    private static BadRequestException ChallengeFailed() =>
        new("Verification challenge failed", ErrorCodeEnum.ValidationError);

    private sealed class SiteVerifyResponse
    {
        public bool Success { get; set; }
    }
}
