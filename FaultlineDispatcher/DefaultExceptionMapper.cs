namespace FaultlineDispatcher;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Built-in mapper, always evaluated after host-registered mappers. Handles framework exceptions whose message
/// must never reach the caller.
/// </summary>
internal sealed class DefaultExceptionMapper : IExceptionMapper
{
    public string? Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            BadHttpRequestException { StatusCode: StatusCodes.Status408RequestTimeout } => FaultlineResponseKeys.RequestTimeout,
            BadHttpRequestException => FaultlineResponseKeys.BadRequest,
            TimeoutException => FaultlineResponseKeys.DependencyTimeout,
            _ => null
        };
    }
}
