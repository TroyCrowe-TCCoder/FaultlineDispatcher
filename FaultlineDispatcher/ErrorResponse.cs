namespace FaultlineDispatcher;

/// <summary>
/// A configurable error response definition. Every property is optional: a value that is not
/// supplied falls back to the library default for the same response key.
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
