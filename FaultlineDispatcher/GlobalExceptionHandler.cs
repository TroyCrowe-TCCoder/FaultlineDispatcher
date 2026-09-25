namespace FaultlineDispatcher;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Centralized global error handler. Logs every unhandled request exception exactly once with
/// structured metadata, then produces a <see cref="ProblemDetails"/> response carrying the
/// request's correlation identifier as <c>traceId</c>.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    // Observability: standard structured message for unhandled request failures.
    private static readonly Action<ILogger, string, string, Exception?> _logUnhandledRequestException =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1001, nameof(GlobalExceptionHandler)),
            "Unhandled exception for HTTP {Method} {Path}");

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(problemDetailsService);

        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        // Observability: capture request metadata for the unhandled exception; this is the single
        // point where unhandled exceptions are logged.
        _logUnhandledRequestException(_logger, httpContext.Request.Method, httpContext.Request.Path, exception);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "If this continues, contact support."
            }
        }).ConfigureAwait(false);
    }
}
