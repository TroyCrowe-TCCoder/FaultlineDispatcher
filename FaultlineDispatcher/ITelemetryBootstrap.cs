namespace FaultlineDispatcher;

using Microsoft.Extensions.Configuration;
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
    /// Registers the telemetry provider's services into <paramref name="services"/>. Must be
    /// safe to call with no telemetry connection string/key configured (no-op or local-only
    /// behavior expected in that case).
    /// </summary>
    void Configure(IServiceCollection services, IConfiguration configuration);
}
