namespace FaultlineDispatcher;

/// <summary>
/// A response definition supplied by the host. <see cref="Title"/> is required for every key. <see cref="Status"/>
/// is required except for <see cref="FaultlineResponseKeys.Unauthorized"/> and
/// <see cref="FaultlineResponseKeys.Forbidden"/>, whose status is fixed. <see cref="Detail"/> is optional.
/// Write messages a caller can act on without exposing internal information: no exception text, identifiers,
/// table names or stack details.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>HTTP status code (400-599) returned to the caller.</summary>
    public int? Status { get; set; }

    /// <summary>Short <c>ProblemDetails</c> title returned to the caller.</summary>
    public string? Title { get; set; }

    /// <summary>User-facing <c>ProblemDetails</c> detail returned to the caller.</summary>
    public string? Detail { get; set; }
}
