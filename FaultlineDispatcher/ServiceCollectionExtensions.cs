namespace FaultlineDispatcher;

using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI extensions for registering FaultlineDispatcher's core error-handling and
/// authorization-failure-shaping services, plus any registered <see cref="ITelemetryBootstrap"/>
/// plugin.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ProblemDetails"/>, the <see cref="GlobalExceptionHandler"/>, and the
    /// <see cref="AuthorizationProblemDetailsResultHandler"/>. Call <c>app.UseExceptionHandler()</c>
    /// in the middleware pipeline after calling this.
    /// </summary>
    public static IServiceCollection AddFaultlineDispatcher(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationProblemDetailsResultHandler>();

        return services;
    }

    /// <summary>
    /// Invokes <see cref="ITelemetryBootstrap.Configure"/> for a specific telemetry plugin
    /// implementation. No-op if the consumer chooses not to register a telemetry plugin at all.
    /// </summary>
    public static IServiceCollection AddTelemetryBootstrap<TBootstrap>(this IServiceCollection services, IConfiguration configuration)
        where TBootstrap : ITelemetryBootstrap, new()
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        new TBootstrap().Configure(services, configuration);

        return services;
    }
}
