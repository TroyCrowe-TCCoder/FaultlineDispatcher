namespace FaultlineDispatcher;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Centralized global error handler. Maps the exception to a safe, configurable response (status, title, detail)
/// through the registered <see cref="IExceptionMapper"/> chain, logs the failure exactly once with structured
/// metadata, and writes a <see cref="ProblemDetails"/> response. Exception text is never returned to the caller.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    // Non-standard but widely used status for a client that closed the connection.
    private const int ClientClosedRequest = 499;

    // Observability: standard structured message for failed requests. Single point where request exceptions are logged.
    private static readonly Action<ILogger, string, string, int, string, Exception?> _logRequestException =
        LoggerMessage.Define<string, string, int, string>(
            LogLevel.Error,
            new EventId(1001, nameof(GlobalExceptionHandler)),
            "Unhandled exception for HTTP {Method} {Path} mapped to {StatusCode} ({ResponseKey})");

    private static readonly Action<ILogger, string, string, int, string, Exception?> _logClientRequestException =
        LoggerMessage.Define<string, string, int, string>(
            LogLevel.Warning,
            new EventId(1001, nameof(GlobalExceptionHandler)),
            "Request exception for HTTP {Method} {Path} mapped to {StatusCode} ({ResponseKey})");

    private static readonly Action<ILogger, string, Exception?> _logMapperFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1002, "ExceptionMapperFailed"),
            "Exception mapper {Mapper} threw and was skipped.");

    private static readonly IExceptionMapper DefaultMapper = new DefaultExceptionMapper();

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IExceptionMapper[] _mappers;
    private readonly FaultlineOptions _options;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService,
        IOptions<FaultlineOptions> options,
        IEnumerable<IExceptionMapper>? mappers = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(problemDetailsService);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _problemDetailsService = problemDetailsService;
        _mappers = [.. mappers ?? [], DefaultMapper];
        _options = options.Value;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        // The caller disconnected; there is nobody to answer and nothing to log as an error.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var key = ResolveKey(exception);
        var response = ErrorResponseCatalog.Resolve(key, _options);

        // Observability: capture request metadata for the exception; this is the single point where
        // request exceptions are logged.
        if (response.Status >= StatusCodes.Status500InternalServerError)
        {
            _logRequestException(_logger, httpContext.Request.Method, httpContext.Request.Path, response.Status, key, exception);
        }
        else
        {
            _logClientRequestException(_logger, httpContext.Request.Method, httpContext.Request.Path, response.Status, key, exception);
        }

        httpContext.Response.StatusCode = response.Status;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = response.Status,
                Title = response.Title,
                Detail = response.Detail
            }
        }).ConfigureAwait(false);
    }

    // Outer exception first, then each inner exception, so a wrapped SqlException is still recognized.
    private string ResolveKey(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            foreach (var mapper in _mappers)
            {
                string? key;
                try
                {
                    key = mapper.Map(current);
                }
                catch (Exception mapperException)
                {
                    _logMapperFailed(_logger, mapper.GetType().Name, mapperException);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(key))
                {
                    return key;
                }
            }
        }

        return FaultlineResponseKeys.Unexpected;
    }
}
