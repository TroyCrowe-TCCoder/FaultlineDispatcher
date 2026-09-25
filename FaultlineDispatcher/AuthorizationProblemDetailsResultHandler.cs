namespace FaultlineDispatcher;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<AuthorizationProblemDetailsResultHandler> _logger;

    public AuthorizationProblemDetailsResultHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<AuthorizationProblemDetailsResultHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(problemDetailsService);
        ArgumentNullException.ThrowIfNull(logger);

        _problemDetailsService = problemDetailsService;
        _logger = logger;
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

            await context.ChallengeAsync();
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                "The request requires a valid authenticated user.");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            var path = context.Request.Path.Value ?? "/";
            var userId = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "anonymous";

            AuthorizationForbiddenLogMessage(_logger, userId, path, null);

            await context.ForbidAsync();
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status403Forbidden,
                "You do not have permission to perform this action.",
                "The current user does not satisfy the authorization requirements for this operation.");
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;

        var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            }
        }).ConfigureAwait(false);

        if (!written)
        {
            ProblemDetailsWriteFailedLogMessage(_logger, statusCode, null);
        }
    }
}
