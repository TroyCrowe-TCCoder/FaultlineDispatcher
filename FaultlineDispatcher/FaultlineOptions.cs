namespace FaultlineDispatcher;

/// <summary>
/// Responses FaultlineDispatcher returns to callers. The library ships no response text: the host defines every
/// response in code through <see cref="ServiceCollectionExtensions.AddFaultlineDispatcher"/>. The options are
/// validated on start.
/// </summary>
public sealed class FaultlineOptions
{
    /// <summary>
    /// Response definitions keyed by response key (see <see cref="FaultlineResponseKeys"/>). Every well-known key
    /// must be defined. A host-specific key may also be added for use by its own <see cref="IExceptionMapper"/>.
    /// </summary>
    public Dictionary<string, ErrorResponse> Responses { get; } = new(StringComparer.OrdinalIgnoreCase);
}
