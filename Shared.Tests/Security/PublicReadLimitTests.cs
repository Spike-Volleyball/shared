using System.Net;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Threading.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Security.PublicReads;
using Shared.Tests.Services.Analytics;

namespace Shared.Tests.Security;

[TestFixture]
[Category("Unit")]
public class PublicReadLimitTests
{
    private const string RendererSecret = "the-renderer-secret";
    private const int PermitLimit = 2;
    private const string VisitorAddress = "203.0.113.10";
    private const string GatewayAddress = "172.18.0.5";

    [Test]
    public void GetPartition_ASignedOutVisitor_IsCountedByTheAddressTheGatewayNamed()
    {
        // Arrange
        var request = Request(clientAddress: VisitorAddress, connection: GatewayAddress);

        // Act
        var partition = Policy().GetPartition(request);

        // Assert
        partition.PartitionKey.Should().Be(VisitorAddress);
        IsLimited(partition).Should().BeTrue();
    }

    [Test]
    public void GetPartition_WithNoAddressFromTheGateway_CountsTheConnection()
    {
        // Act
        var partition = Policy().GetPartition(Request(clientAddress: null, connection: "198.51.100.7"));

        // Assert
        partition.PartitionKey.Should().Be("198.51.100.7");
        IsLimited(partition).Should().BeTrue();
    }

    [Test]
    public void GetPartition_AnAddressTheGatewayCannotHaveWritten_CountsTheConnection()
    {
        // Act
        var partition = Policy().GetPartition(Request(clientAddress: "not an address", connection: "198.51.100.7"));

        // Assert
        partition.PartitionKey.Should().Be("198.51.100.7");
    }

    [Test]
    public void GetPartition_TheRendererWithTheSecret_IsNotCounted()
    {
        // Act
        var partition = Policy().GetPartition(Request(renderer: RendererSecret));

        // Assert
        IsLimited(partition).Should().BeFalse();
    }

    [Test]
    public void GetPartition_AWrongSecretOfTheSameLength_IsCounted()
    {
        // Arrange
        var wrong = RendererSecret.ToUpperInvariant();

        // Act
        var partition = Policy().GetPartition(Request(renderer: wrong));

        // Assert
        wrong.Length.Should().Be(RendererSecret.Length);
        IsLimited(partition).Should().BeTrue();
    }

    [TestCase("the-renderer-secre")]
    [TestCase("the-renderer-secret-and-more")]
    [TestCase("")]
    public void GetPartition_ASecretOfAnotherLength_IsCounted(string presented)
    {
        // Act
        var partition = Policy().GetPartition(Request(renderer: presented));

        // Assert
        IsLimited(partition).Should().BeTrue();
    }

    [Test]
    public void GetPartition_WithoutTheRendererHeader_IsCounted()
    {
        // Act
        var partition = Policy().GetPartition(Request(renderer: null));

        // Assert
        IsLimited(partition).Should().BeTrue();
    }

    /// <summary>
    /// The limit is for signed-out reads. A signed-in app shares its address with everyone behind the same
    /// carrier or venue network, and it is known by its account, not by that address.
    /// </summary>
    [Test]
    public void GetPartition_ASignedInCaller_IsNotCounted()
    {
        // Arrange
        var request = Request();
        request.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Bearer"));

        // Act
        var partition = Policy().GetPartition(request);

        // Assert
        IsLimited(partition).Should().BeFalse();
    }

    [TestCase(null)]
    [TestCase("")]
    public void GetPartition_WhenNoRendererSecretIsConfigured_CountsNobody(string? configured)
    {
        // Act
        var partition = Policy(configured).GetPartition(Request());

        // Assert
        IsLimited(partition).Should().BeFalse();
    }

    [Test]
    public void Constructor_WhenNoRendererSecretIsConfigured_WarnsOnce()
    {
        // Arrange
        var logger = new RecordingLogger<PublicReadLimit>();

        // Act
        _ = Policy(null, logger);

        // Assert
        logger.Entries.Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Warning);
        logger.Entries[0].Message.Should().Contain(PublicReadSettings.SectionName);
    }

    [Test]
    public void Constructor_WithARendererSecret_SaysNothing()
    {
        // Arrange
        var logger = new RecordingLogger<PublicReadLimit>();

        // Act
        _ = Policy(RendererSecret, logger);

        // Assert
        logger.Entries.Should().BeEmpty();
    }

    [Test]
    public async Task OnRejected_Answers429WithRetryAfter_AsAProblemDocument()
    {
        // Arrange
        var policy = Policy();
        var request = Request();
        request.Response.Body = new MemoryStream();
        var partition = policy.GetPartition(request);
        using var limiter = partition.Factory(partition.PartitionKey);
        for (var i = 0; i < PermitLimit; i++)
            limiter.AttemptAcquire().Dispose();
        using var refused = limiter.AttemptAcquire();

        // Act
        await policy.OnRejected!(new OnRejectedContext { HttpContext = request, Lease = refused }, CancellationToken.None);

        // Assert
        request.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        request.Response.Headers.RetryAfter.ToString().Should().Be("60");
        request.Response.ContentType.Should().StartWith("application/problem+json");
        request.Response.Body.Position = 0;
        var problem = await JsonNode.ParseAsync(request.Response.Body);
        problem!["status"]!.GetValue<int>().Should().Be(StatusCodes.Status429TooManyRequests);
        problem["code"]!.GetValue<string>().Should().Be("RATE_LIMITED");
        problem["type"]!.GetValue<string>().Should().Be("https://api.volleyspike.app/errors/too-many-requests");
    }

    private static PublicReadLimit Policy(string? rendererSecret = RendererSecret, ILogger<PublicReadLimit>? logger = null) =>
        new(
            new OptionsWrapper<PublicReadSettings>(new PublicReadSettings { RendererSecret = rendererSecret, PermitLimit = PermitLimit }),
            logger ?? new RecordingLogger<PublicReadLimit>());

    private static DefaultHttpContext Request(
        string? clientAddress = VisitorAddress, string connection = GatewayAddress, string? renderer = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(connection);
        if (clientAddress is not null)
            context.Request.Headers[PublicReadLimit.ClientAddressHeader] = clientAddress;
        if (renderer is not null)
            context.Request.Headers[PublicReadLimit.RendererHeader] = renderer;
        return context;
    }

    /// <summary>Whether the partition's limiter refuses a read past the limit.</summary>
    private static bool IsLimited(RateLimitPartition<string> partition)
    {
        using var limiter = partition.Factory(partition.PartitionKey);
        for (var i = 0; i < PermitLimit; i++)
            limiter.AttemptAcquire().Dispose();

        using var beyond = limiter.AttemptAcquire();
        return !beyond.IsAcquired;
    }
}
