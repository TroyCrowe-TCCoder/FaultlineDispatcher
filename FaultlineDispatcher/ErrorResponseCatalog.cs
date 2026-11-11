namespace FaultlineDispatcher;

using Microsoft.AspNetCore.Http;

/// <summary>A fully resolved response: status, title and detail ready to write. A null title is filled in by ASP.NET Core.</summary>
internal readonly record struct ResolvedResponse(int Status, string? Title, string? Detail);

/// <summary>Looks up host-supplied responses. The library holds no response text of its own.</summary>
internal static class ErrorResponseCatalog
{
    /// <summary>Keys whose status is fixed by the protocol rather than supplied by the host.</summary>
    internal static readonly IReadOnlyDictionary<string, int> FixedStatusKeys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        [FaultlineResponseKeys.Unauthorized] = StatusCodes.Status401Unauthorized,
        [FaultlineResponseKeys.Forbidden] = StatusCodes.Status403Forbidden
    };

    /// <summary>Every key the host must define.</summary>
    internal static readonly IReadOnlyList<string> RequiredKeys =
    [
        FaultlineResponseKeys.BadRequest,
        FaultlineResponseKeys.ContentNotFound,
        FaultlineResponseKeys.Conflict,
        FaultlineResponseKeys.RequestTimeout,
        FaultlineResponseKeys.Unavailable,
        FaultlineResponseKeys.DependencyTimeout,
        FaultlineResponseKeys.Unexpected,
        FaultlineResponseKeys.Unauthorized,
        FaultlineResponseKeys.Forbidden
    ];

    /// <summary>
    /// Resolves <paramref name="key"/> from the host's definitions. A key the host did not define falls back to the
    /// host's Unexpected response. If that is missing too (options that were never validated), a bare 500 with no
    /// text is returned so the exception handler itself never throws.
    /// </summary>
    internal static ResolvedResponse Resolve(string key, FaultlineOptions options)
    {
        if (FixedStatusKeys.ContainsKey(key))
        {
            return ResolveFixed(key, options);
        }

        if (TryGet(key, options, out var response))
        {
            return response;
        }

        if (TryGet(FaultlineResponseKeys.Unexpected, options, out var unexpected))
        {
            return unexpected;
        }

        return new ResolvedResponse(StatusCodes.Status500InternalServerError, null, null);
    }

    /// <summary>Resolves the host's title and detail for a key whose status is fixed.</summary>
    internal static ResolvedResponse ResolveFixed(string key, FaultlineOptions options)
    {
        var status = FixedStatusKeys[key];

        return options.Responses.TryGetValue(key, out var entry) && entry is not null
            ? new ResolvedResponse(status, NullIfBlank(entry.Title), entry.Detail)
            : new ResolvedResponse(status, null, null);
    }

    private static bool TryGet(string key, FaultlineOptions options, out ResolvedResponse response)
    {
        response = default;

        if (!options.Responses.TryGetValue(key, out var entry)
            || entry is null
            || entry.Status is not (>= 400 and <= 599)
            || string.IsNullOrWhiteSpace(entry.Title))
        {
            return false;
        }

        response = new ResolvedResponse(entry.Status.Value, entry.Title, entry.Detail);
        return true;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
