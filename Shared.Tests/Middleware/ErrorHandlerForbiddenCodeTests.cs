using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Middleware;

namespace Shared.Tests.Middleware;

/// <summary>
/// The code a 403 carries on the wire. It has always been FORBIDDEN whatever code the exception was thrown with, and
/// clients read it so. A refusal a screen tells apart from any other 403, to show one line where an error would be, is
/// the exception: it goes out under its own code.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ErrorHandlerForbiddenCodeTests
{
    private static async Task<(int Status, JsonElement Body)> RefusedWithAsync(Exception thrown)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ErrorHandlerMiddleware(
            _ => throw thrown, NullLogger<ErrorHandlerMiddleware>.Instance, Substitute.For<IHostEnvironment>());

        await middleware.Invoke(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, document.RootElement.Clone());
    }

    [Test]
    public async Task Invoke_AForbiddenCodedParticipantsHidden_IsAnswered403UnderThatCode()
    {
        // Act
        var (status, body) = await RefusedWithAsync(
            new ForbiddenException("Only the people connected to this match can see who is on it", ErrorCodeEnum.ParticipantsHidden));

        // Assert - still a forbidden problem, with the code a client tells it apart by
        status.Should().Be(403);
        body.GetProperty("code").GetString().Should().Be("PARTICIPANTS_HIDDEN");
        body.GetProperty("title").GetString().Should().Be("Forbidden");
        body.GetProperty("type").GetString().Should().EndWith("/errors/forbidden");
        body.GetProperty("detail").GetString().Should().Be("Only the people connected to this match can see who is on it");
    }

    [TestCase(ErrorCodeEnum.Forbidden)]
    [TestCase(ErrorCodeEnum.InsufficientPermissions)]
    [TestCase(ErrorCodeEnum.NotGuardian)]
    [TestCase(ErrorCodeEnum.MessagingBlocked)]
    [TestCase(ErrorCodeEnum.GuardianAccessDenied)]
    public async Task Invoke_AnyOtherForbidden_IsStillAnsweredUnderForbidden(ErrorCodeEnum thrownWith)
    {
        // Act
        var (status, body) = await RefusedWithAsync(new ForbiddenException("No", thrownWith));

        // Assert - clients that read FORBIDDEN for a 403 keep reading it
        status.Should().Be(403);
        body.GetProperty("code").GetString().Should().Be("FORBIDDEN");
    }

    [Test]
    public async Task Invoke_AForbiddenWithNoCode_IsAnsweredUnderForbidden()
    {
        // Act
        var (status, body) = await RefusedWithAsync(new ForbiddenException("No"));

        // Assert
        status.Should().Be(403);
        body.GetProperty("code").GetString().Should().Be("FORBIDDEN");
    }
}
