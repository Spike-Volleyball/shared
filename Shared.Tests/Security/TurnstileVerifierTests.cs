using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Security.Turnstile;

namespace Shared.Tests.Security;

[TestFixture]
[Category("Unit")]
public class TurnstileVerifierTests
{
    private const string Secret = "the-secret";
    private const string Token = "the-token";

    [Test]
    public async Task RequireChallengePassedAsync_WithNoSecretConfigured_PassesWithoutCallingCloudflare()
    {
        // Arrange
        var handler = new StubHandler(HttpStatusCode.OK, """{"success":false}""");

        // Act
        await Verifier(handler, secret: null).RequireChallengePassedAsync(null, null);

        // Assert
        handler.Requests.Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public async Task RequireChallengePassedAsync_WithoutAToken_Throws(string? token)
    {
        // Act
        var act = () => Verifier(new StubHandler(HttpStatusCode.OK, """{"success":true}""")).RequireChallengePassedAsync(token, null);

        // Assert
        var thrown = await act.Should().ThrowAsync<BadRequestException>();
        thrown.Which.Message.Should().Be("Verification challenge failed");
        thrown.Which.ErrorCode.Should().Be(ErrorCodeEnum.ValidationError);
    }

    [Test]
    public async Task RequireChallengePassedAsync_WhenCloudflareSaysItFailed_Throws()
    {
        // Act
        var act = () => Verifier(new StubHandler(HttpStatusCode.OK, """{"success":false}""")).RequireChallengePassedAsync(Token, null);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("Verification challenge failed");
    }

    [Test]
    public async Task RequireChallengePassedAsync_WhenCloudflareSaysItPassed_SendsTheSecretTokenAndAddress()
    {
        // Arrange
        var handler = new StubHandler(HttpStatusCode.OK, """{"success":true}""");

        // Act
        await Verifier(handler).RequireChallengePassedAsync(Token, "203.0.113.10");

        // Assert
        handler.Requests.Should().ContainSingle().Which.Should()
            .Contain("secret=the-secret").And.Contain("response=the-token").And.Contain("remoteip=203.0.113.10");
    }

    [Test]
    public async Task RequireChallengePassedAsync_WhenCloudflareErrors_AllowsTheRequest()
    {
        // Act
        var act = () => Verifier(new StubHandler(HttpStatusCode.BadGateway, "")).RequireChallengePassedAsync(Token, null);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task RequireChallengePassedAsync_WhenCloudflareIsUnreachable_AllowsTheRequest()
    {
        // Act
        var act = () => Verifier(new StubHandler(new HttpRequestException("down"))).RequireChallengePassedAsync(Token, null);

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static TurnstileVerifier Verifier(StubHandler handler, string? secret = Secret) =>
        new(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(new TurnstileSettings { SecretKey = secret }),
            NullLogger<TurnstileVerifier>.Instance);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        private readonly Exception? _failure;

        public List<string> Requests { get; } = [];

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public StubHandler(Exception failure)
        {
            _failure = failure;
            _body = "";
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return _failure is null
                ? new HttpResponseMessage(_status) { Content = new StringContent(_body) }
                : throw _failure;
        }
    }
}
