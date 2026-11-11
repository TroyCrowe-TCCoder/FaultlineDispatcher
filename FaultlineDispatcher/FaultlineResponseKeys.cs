namespace FaultlineDispatcher;

/// <summary>
/// Names of the well-known response definitions. An <see cref="IExceptionMapper"/> returns one of these
/// keys (or a host-defined key); the status code and message are then resolved from
/// <see cref="FaultlineOptions.Responses"/>. The host must define every key listed here.
/// </summary>
public static class FaultlineResponseKeys
{
    /// <summary>400 - the request is invalid.</summary>
    public const string BadRequest = "BadRequest";

    /// <summary>400 - the requested content could not be found (a write that targets a missing record).</summary>
    public const string ContentNotFound = "ContentNotFound";

    /// <summary>409 - the request conflicts with existing data. The message is deliberately neutral.</summary>
    public const string Conflict = "Conflict";

    /// <summary>408 - the request timed out.</summary>
    public const string RequestTimeout = "RequestTimeout";

    /// <summary>503 - a dependency is temporarily unavailable.</summary>
    public const string Unavailable = "Unavailable";

    /// <summary>504 - a dependency did not respond in time.</summary>
    public const string DependencyTimeout = "DependencyTimeout";

    /// <summary>500 - anything that is not recognized.</summary>
    public const string Unexpected = "Unexpected";

    /// <summary>401 - authentication is required. The status is fixed; the host supplies the title and detail.</summary>
    public const string Unauthorized = "Unauthorized";

    /// <summary>403 - the user is not permitted. The status is fixed; the host supplies the title and detail.</summary>
    public const string Forbidden = "Forbidden";
}
