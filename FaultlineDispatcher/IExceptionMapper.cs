namespace FaultlineDispatcher;

/// <summary>
/// Maps an exception to a response key. Register implementations with
/// <see cref="ServiceCollectionExtensions.AddExceptionMapper{TMapper}"/>. Mappers are singletons and run in
/// registration order, followed by the built-in defaults.
/// </summary>
public interface IExceptionMapper
{
    /// <summary>
    /// Returns the response key for <paramref name="exception"/>, or <see langword="null"/> when this mapper does
    /// not handle it. Inspect only the supplied exception; <see cref="GlobalExceptionHandler"/> walks inner
    /// exceptions itself. Never return exception text: the caller-facing message comes from configuration.
    /// </summary>
    string? Map(Exception exception);
}
