namespace FaultlineDispatcher;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Vendor-neutral telemetry bootstrap plugin contract. Implementations register whatever
/// telemetry/APM SDK they wrap (Application Insights, New Relic, etc.) into the service
/// collection. The core package has no dependency on any specific vendor; consumers opt into
/// a vendor by registering the corresponding plugin package's implementation.
/// </summary>
public interface ITelemetryBootstrap
{
    /// <summary>
    /// Registers the telemetry provider's services into <paramref name="services"/>. Implementations take their
    /// settings from the host through their own constructor or options delegate and never read configuration.
    /// </summary>
    void Configure(IServiceCollection services);
}
