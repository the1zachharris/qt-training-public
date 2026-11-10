using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace QT.PizzaOrdering.API.Middleware;

/// <summary>
/// Global exception handler that catches unhandled exceptions and converts them to
/// appropriate HTTP problem details responses.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "IExceptionHandler class.")]
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <remarks>
    /// Exception mappings:
    /// - BadHttpRequestException, ArgumentException, JsonException → 400 Bad Request
    /// - All others → 500 Internal Server Error, with a generic detail so internals are not exposed
    /// </remarks>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        bool isBadRequest = exception is BadHttpRequestException or ArgumentException or JsonException;

        ProblemDetails problemDetails = new()
        {
            Status = isBadRequest ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError,
            Title = isBadRequest ? nameof(StatusCodes.Status400BadRequest) : nameof(StatusCodes.Status500InternalServerError),
            Detail = isBadRequest ? exception.Message : "Something went wrong on our end. Please try again.",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
