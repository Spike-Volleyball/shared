using System.Text.Json.Serialization;
using Shared.Enums;

namespace Shared.DTOs.Errors;

public class ProblemDetailsResponse
{
    private const string BaseTypeUrl = "https://api.volleyspike.app/errors";

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; set; }

    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FieldError>? Errors { get; set; }

    [JsonPropertyName("debug")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DebugInfo? Debug { get; set; }

    public ProblemDetailsResponse() { }

    private ProblemDetailsResponse(string type, string title, int status, string code, string? detail = null)
    {
        Type = $"{BaseTypeUrl}/{type}";
        Title = title;
        Status = status;
        Code = code;
        Detail = detail;
    }

    public static ProblemDetailsResponse ValidationError(List<FieldError> errors)
    {
        return new ProblemDetailsResponse(
            type: "validation-error",
            title: "Validation Failed",
            status: 400,
            code: "VALIDATION_ERROR"
        )
        {
            Errors = errors
        };
    }

    public static ProblemDetailsResponse ValidationError(string detail, List<FieldError> errors)
    {
        return new ProblemDetailsResponse(
            type: "validation-error",
            title: "Validation Failed",
            status: 400,
            code: "VALIDATION_ERROR",
            detail: detail
        )
        {
            Errors = errors
        };
    }

    public static ProblemDetailsResponse BadRequest(string code, string detail)
    {
        return new ProblemDetailsResponse(
            type: "bad-request",
            title: "Bad Request",
            status: 400,
            code: code,
            detail: detail
        );
    }

    public static ProblemDetailsResponse Unauthorized(string code, string detail)
    {
        return new ProblemDetailsResponse(
            type: "unauthorized",
            title: "Unauthorized",
            status: 401,
            code: code,
            detail: detail
        );
    }

    public static ProblemDetailsResponse Forbidden(string detail)
    {
        return new ProblemDetailsResponse(
            type: "forbidden",
            title: "Forbidden",
            status: 403,
            code: "FORBIDDEN",
            detail: detail
        );
    }

    public static ProblemDetailsResponse NotFound(string code, string detail)
    {
        return new ProblemDetailsResponse(
            type: "not-found",
            title: "Not Found",
            status: 404,
            code: code,
            detail: detail
        );
    }

    public static ProblemDetailsResponse Conflict(string detail)
    {
        return new ProblemDetailsResponse(
            type: "conflict",
            title: "Conflict",
            status: 409,
            code: "CONFLICT",
            detail: detail
        );
    }

    public static ProblemDetailsResponse Conflict(string code, string detail)
    {
        return new ProblemDetailsResponse(
            type: "conflict",
            title: "Conflict",
            status: 409,
            code: code,
            detail: detail
        );
    }

    public static ProblemDetailsResponse TooManyRequests(string detail)
    {
        return new ProblemDetailsResponse(
            type: "too-many-requests",
            title: ErrorCodeEnum.RateLimited.ToTitle(),
            status: 429,
            code: ErrorCodeEnum.RateLimited.ToStringCode(),
            detail: detail
        );
    }

    public static ProblemDetailsResponse InternalError(string detail)
    {
        return new ProblemDetailsResponse(
            type: "internal-error",
            title: "Internal Server Error",
            status: 500,
            code: "INTERNAL_ERROR",
            detail: detail
        );
    }

    public static ProblemDetailsResponse InternalError(string detail, DebugInfo? debug)
    {
        return new ProblemDetailsResponse(
            type: "internal-error",
            title: "Internal Server Error",
            status: 500,
            code: "INTERNAL_ERROR",
            detail: detail
        )
        {
            Debug = debug
        };
    }
}

public class DebugInfo
{
    [JsonPropertyName("exceptionType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExceptionType { get; set; }

    [JsonPropertyName("stackTrace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StackTrace { get; set; }

    [JsonPropertyName("innerException")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InnerException { get; set; }
}
