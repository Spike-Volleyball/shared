using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Shared.DTOs.Errors;
using Shared.Enums;
using Shared.Exceptions;

namespace Shared.Middleware;

[ExcludeFromCodeCoverage]
public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ErrorHandlerMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            HandleAbandonedRequest(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// The caller hung up and the work stopped with it: nothing failed, and nobody is left to
    /// answer. 499 rather than 500, so the request counts as the caller's in the metrics the
    /// availability alerts read, and no error is logged for Sentry to raise.
    /// </summary>
    private void HandleAbandonedRequest(HttpContext context)
    {
        _logger.LogInformation(
            "Request abandoned by the caller. Method: {Method}, Path: {Path}",
            context.Request.Method, context.Request.Path);

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        var response = ex switch
        {
            ValidationException validationEx => HandleValidationException(validationEx),
            ExceptionWithStatusAndErrorCodes exWithCodes => HandleExceptionWithStatusAndErrorCodes(exWithCodes),
            BadHttpRequestException badRequest => HandleBadHttpRequestException(badRequest),
            _ => HandleUnexpectedException(ex)
        };

        // Log based on status code severity
        if (response.Status >= 500)
        {
            _logger.LogError(ex,
                "Unhandled exception. Method: {Method}, Path: {Path}, Status: {Status}, Code: {Code}",
                requestMethod, requestPath, response.Status, response.Code);
        }
        else if (response.Status >= 400)
        {
            _logger.LogWarning(
                "Client error. Method: {Method}, Path: {Path}, Status: {Status}, Code: {Code}, Detail: {Detail}",
                requestMethod, requestPath, response.Status, response.Code, response.Detail);
        }

        context.Response.StatusCode = response.Status;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static ProblemDetailsResponse HandleValidationException(ValidationException ex)
    {
        return ProblemDetailsResponse.ValidationError(ex.Message, ex.FieldErrors);
    }

    private static ProblemDetailsResponse HandleExceptionWithStatusAndErrorCodes(ExceptionWithStatusAndErrorCodes ex)
    {
        var statusCode = (int)ex.StatusCode;
        var stringCode = ex.ErrorCode.ToStringCode();

        return statusCode switch
        {
            400 => ProblemDetailsResponse.BadRequest(stringCode, ex.Message),
            401 => ProblemDetailsResponse.Unauthorized(stringCode, ex.Message),
            403 => ProblemDetailsResponse.Forbidden(ex.Message),
            404 => ProblemDetailsResponse.NotFound(stringCode, ex.Message),
            409 => ProblemDetailsResponse.Conflict(stringCode, ex.Message),
            _ => new ProblemDetailsResponse
            {
                Type = $"https://api.volleyspike.app/errors/{stringCode.ToLowerInvariant()}",
                Title = ex.ErrorCode.ToTitle(),
                Status = statusCode,
                Code = stringCode,
                Detail = ex.Message
            }
        };
    }

    private ProblemDetailsResponse HandleBadHttpRequestException(BadHttpRequestException ex)
    {
        // Handle .NET built-in bad request errors (JSON parsing, etc.)
        return ProblemDetailsResponse.BadRequest(
            "BAD_REQUEST",
            _environment.IsDevelopment() ? ex.Message : "The request could not be processed."
        );
    }

    private ProblemDetailsResponse HandleUnexpectedException(Exception ex)
    {
        DebugInfo? debug = null;
        if (_environment.IsDevelopment())
        {
            debug = new DebugInfo
            {
                ExceptionType = ex.GetType().Name,
                StackTrace = ex.StackTrace,
                InnerException = ex.InnerException?.Message
            };
        }

        var message = _environment.IsDevelopment()
            ? ex.Message
            : "An unexpected error occurred. Please try again later.";

        return ProblemDetailsResponse.InternalError(message, debug);
    }
}
