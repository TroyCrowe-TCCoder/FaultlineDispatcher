namespace FaultlineDispatcher;

using Microsoft.AspNetCore.Http;

/// <summary>A fully resolved response: status, title and detail ready to write.</summary>
internal readonly record struct ResolvedResponse(int Status, string Title, string? Detail);

/// <summary>Library default responses and the per-field merge with host configuration.</summary>
internal static class ErrorResponseCatalog
{
    private static readonly ResolvedResponse UnexpectedDefault = new(
        StatusCodes.Status500InternalServerError,
        "An unexpected error occurred.",
        "If this continues, contact support.");

    private static readonly Dictionary<string, ResolvedResponse> Defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        [FaultlineResponseKeys.BadRequest] = new(
            StatusCodes.Status400BadRequest,
            "The request is invalid.",
            "Check the request and try again."),
        [FaultlineResponseKeys.ContentNotFound] = new(
            StatusCodes.Status400BadRequest,
            "The requested content could not be found.",
            null),
        [FaultlineResponseKeys.Conflict] = new(
            StatusCodes.Status409Conflict,
            "This request conflicts with existing data.",
            null),
        [FaultlineResponseKeys.RequestTimeout] = new(
            StatusCodes.Status408RequestTimeout,
            "The request timed out.",
            "Try again."),
        [FaultlineResponseKeys.Unavailable] = new(
            StatusCodes.Status503ServiceUnavailable,
            "The service is temporarily unavailable.",
            "Try again shortly."),
        [FaultlineResponseKeys.DependencyTimeout] = new(
            StatusCodes.Status504GatewayTimeout,
            "The service took too long to respond.",
            "Try again shortly."),
        [FaultlineResponseKeys.Unexpected] = UnexpectedDefault
    };

    /// <summary>
    /// Resolves <paramref name="key"/>: host configuration overrides the library default field by field. A key with
    /// no library default (a host-defined key) falls back to the Unexpected status and title and no detail.
    /// </summary>
    internal static ResolvedResponse Resolve(string key, FaultlineOptions options)
    {
        var baseline = Defaults.TryGetValue(key, out var known)
            ? known
            : UnexpectedDefault with { Detail = null };

        if (!options.Responses.TryGetValue(key, out var overrides) || overrides is null)
        {
            return baseline;
        }

        var status = overrides.Status is >= 400 and <= 599 ? overrides.Status.Value : baseline.Status;
        var title = string.IsNullOrWhiteSpace(overrides.Title) ? baseline.Title : overrides.Title;
        var detail = overrides.Detail ?? baseline.Detail;

        return new ResolvedResponse(status, title, detail);
    }
}
