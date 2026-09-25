namespace FaultlineDispatcher.Telemetry.ApplicationInsights;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers Application Insights telemetry. Safe to register with no connection string
/// configured yet; the Application Insights SDK no-ops locally in that case.
/// </summary>
public sealed class ApplicationInsightsTelemetryBootstrap : ITelemetryBootstrap
{
    public void Configure(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddApplicationInsightsTelemetry();
    }
}
