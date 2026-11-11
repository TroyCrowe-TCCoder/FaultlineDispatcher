namespace FaultlineDispatcher;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Shapes authorization middleware outcomes (challenge/forbidden) into <see cref="ProblemDetails"/>
/// responses instead of empty status codes, and logs the corresponding security events.
/// </summary>
public sealed class AuthorizationProblemDetailsResultHandler : IAuthorizationMiddlewareResultHandler
{
    private static readonly Action<ILogger, int, Exception?> ProblemDetailsWriteFailedLogMessage =
        LoggerMessage.Define<int>(
            LogLevel.Warning,
            new EventId(1, nameof(WriteProblemDetailsAsync)),
            "ProblemDetails response could not be written for status code {StatusCode}.");

    private static readonly Action<ILogger, string, Exception?> AuthenticationChallengeLogMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2, "SecurityEvent.AuthenticationChallenge"),
            "Authentication challenge issued for request to {Path}.");

    private static readonly Action<ILogger, string, string, Exception?> AuthorizationForbiddenLogMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(3, "SecurityEvent.AuthorizationForbidden"),
            "Authorization forbidden for user {UserId} on request to {Path}.");

    private static readonly Action<ILogger, string, Exception?> DenialObserverFailedLogMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(4, "AuthorizationDenialObserverFailed"),
            "Authorization denial observer {Observer} threw and was skipped.");

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<AuthorizationProblemDetailsResultHandler> _logger;
    private readonly FaultlineOptions _options;
    private readonly IAuthorizationDenialObserver[] _observers;

    public AuthorizationProblemDetailsResultHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<AuthorizationProblemDetailsResultHandler> logger,
        IOptions<FaultlineOptions> options,
        IEnumerable<IAuthorizationDenialObserver>? observers = null)
    {
        ArgumentNullException.ThrowIfNull(problemDetailsService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _problemDetailsService = problemDetailsService;
        _logger = logger;
        _options = options.Value;
        _observers = [.. observers ?? []];
    }

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(authorizeResult);

        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        if (authorizeResult.Challenged)
        {
            var path = context.Request.Path.Value ?? "/";
            AuthenticationChallengeLogMessage(_logger, path, null);

            await NotifyObserversAsync(context, authorizeResult);
            await context.ChallengeAsync();
            await WriteProblemDetailsAsync(context, ErrorResponseCatalog.ResolveFixed(FaultlineResponseKeys.Unauthorized, _options));
            return;
        }

        if (authorizeResult.Forbidden)
        {
            var path = context.Request.Path.Value ?? "/";
            var userId = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "anonymous";

            AuthorizationForbiddenLogMessage(_logger, userId, path, null);

            await NotifyObserversAsync(context, authorizeResult);
            await context.ForbidAsync();
            await WriteProblemDetailsAsync(context, ErrorResponseCatalog.ResolveFixed(FaultlineResponseKeys.Forbidden, _options));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }

    private async Task NotifyObserversAsync(HttpContext context, PolicyAuthorizationResult authorizeResult)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnDeniedAsync(context, authorizeResult, context.RequestAborted).ConfigureAwait(false);
            }
            catch (Exception observerException)
            {
                DenialObserverFailedLogMessage(_logger, observer.GetType().Name, observerException);
            }
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, ResolvedResponse response)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = response.Status;

        var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = response.Status,
                Title = response.Title,
                Detail = response.Detail
            }
        }).ConfigureAwait(false);

        if (!written)
        {
            ProblemDetailsWriteFailedLogMessage(_logger, response.Status, null);
        }
    }
}
