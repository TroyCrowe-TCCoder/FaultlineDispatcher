namespace FaultlineDispatcher;

using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
/// DI extensions for registering FaultlineDispatcher's core error-handling and
/// authorization-failure-shaping services, plus any registered <see cref="ITelemetryBootstrap"/>
/// plugin.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ProblemDetails"/>, the <see cref="GlobalExceptionHandler"/>, and the
    /// <see cref="AuthorizationProblemDetailsResultHandler"/>. The host defines every response in
    /// <paramref name="configure"/>; the options are validated on start. Call <c>app.UseExceptionHandler()</c>
    /// in the middleware pipeline after calling this.
    /// </summary>
    public static IServiceCollection AddFaultlineDispatcher(this IServiceCollection services, Action<FaultlineOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddProblemDetails();
        services.AddOptions<FaultlineOptions>().Configure(configure).ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<FaultlineOptions>, FaultlineOptionsValidator>());
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationProblemDetailsResultHandler>();

        return services;
    }

    /// <summary>
    /// Registers an <see cref="IExceptionMapper"/> that runs before the built-in mapper, in registration order.
    /// </summary>
    public static IServiceCollection AddExceptionMapper<TMapper>(this IServiceCollection services)
        where TMapper : class, IExceptionMapper
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IExceptionMapper, TMapper>();

        return services;
    }

    /// <summary>
    /// Registers an <see cref="IAuthorizationDenialObserver"/> that is notified on every 401 and 403 produced by the
    /// authorization middleware.
    /// </summary>
    public static IServiceCollection AddAuthorizationDenialObserver<TObserver>(this IServiceCollection services)
        where TObserver : class, IAuthorizationDenialObserver
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAuthorizationDenialObserver, TObserver>();

        return services;
    }

    /// <summary>
    /// Runs a specific telemetry plugin's <see cref="ITelemetryBootstrap.Configure"/>. The host builds the
    /// bootstrap with its own options. No-op if the consumer chooses not to register a telemetry plugin at all.
    /// </summary>
    public static IServiceCollection AddTelemetryBootstrap(this IServiceCollection services, ITelemetryBootstrap bootstrap)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(bootstrap);

        bootstrap.Configure(services);

        return services;
    }
}
