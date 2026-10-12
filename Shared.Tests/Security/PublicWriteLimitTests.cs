using System.Net;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Threading.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Shared.Security.PublicReads;

namespace Shared.Tests.Security;

[TestFixture]
[Category("Unit")]
public class PublicWriteLimitTests
{
    private const int PermitLimit = 2;
    private const string VisitorAddress = "203.0.113.10";
    private const string GatewayAddress = "172.18.0.5";

    [Test]
    public void GetPartition_AVisitor_IsCountedByTheAddressTheGatewayNamed()
    {
        // Act
        var partition = Policy().GetPartition(Request(clientAddress: VisitorAddress));

        // Assert
        partition.PartitionKey.Should().Be(VisitorAddress);
        IsLimited(partition).Should().BeTrue();
    }

    [Test]
    public void GetPartition_WithNoAddressFromTheGateway_CountsTheConnection()
    {
        // Act
        var partition = Policy().GetPartition(Request(clientAddress: null));

        // Assert
        partition.PartitionKey.Should().Be(GatewayAddress);
    }

    [Test]
    public void GetPartition_ASignedInCaller_IsCountedToo()
    {
        // Arrange
        var request = Request(clientAddress: VisitorAddress);
        request.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Bearer"));

        // Act
        var partition = Policy().GetPartition(request);

        // Assert
        IsLimited(partition).Should().BeTrue();
    }

    [Test]
    public void GetPartition_ARequestWithTheReadersRendererHeader_IsCountedToo()
    {
        // Arrange
        var request = Request(clientAddress: VisitorAddress);
        request.Request.Headers[PublicReadLimit.RendererHeader] = "any-secret";

        // Act
        var partition = Policy().GetPartition(request);

        // Assert
        IsLimited(partition).Should().BeTrue();
    }

    [Test]
    public void Defaults_AreStricterThanTheReadLimitPerMinute()
    {
        // Arrange
        var write = new PublicWriteSettings();
        var read = new PublicReadSettings();

        // Act
        var writesPerMinute = write.PermitLimit / write.Window.TotalMinutes;
        var readsPerMinute = read.PermitLimit / read.Window.TotalMinutes;

        // Assert
        writesPerMinute.Should().BeLessThan(readsPerMinute);
    }

    [Test]
    public async Task OnRejected_Answers429WithRetryAfter_AsAProblemDocument()
    {
        // Arrange
        var policy = Policy();
        var request = Request(clientAddress: VisitorAddress);
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
        request.Response.Headers.RetryAfter.ToString().Should().Be("600");
        request.Response.Body.Position = 0;
        var problem = await JsonNode.ParseAsync(request.Response.Body);
        problem!["code"]!.GetValue<string>().Should().Be("RATE_LIMITED");
    }

    private static PublicWriteLimit Policy() =>
        new(new OptionsWrapper<PublicWriteSettings>(new PublicWriteSettings { PermitLimit = PermitLimit }));

    private static DefaultHttpContext Request(string? clientAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(GatewayAddress);
        if (clientAddress is not null)
            context.Request.Headers[PublicReadLimit.ClientAddressHeader] = clientAddress;
        return context;
    }

    private static bool IsLimited(RateLimitPartition<string> partition)
    {
        using var limiter = partition.Factory(partition.PartitionKey);
        for (var i = 0; i < PermitLimit; i++)
            limiter.AttemptAcquire().Dispose();

        using var beyond = limiter.AttemptAcquire();
        return !beyond.IsAcquired;
    }
}
