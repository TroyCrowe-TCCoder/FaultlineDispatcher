namespace FaultlineDispatcher.Telemetry.ApplicationInsights;

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Registration helpers for the Application Insights telemetry plugin.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Application Insights telemetry through FaultlineDispatcher. The host supplies the SDK options in
    /// <paramref name="configure"/>; a connection string is required and validated on start.
    /// </summary>
    public static IServiceCollection AddFaultlineApplicationInsights(this IServiceCollection services, Action<ApplicationInsightsServiceOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddTelemetryBootstrap(new ApplicationInsightsTelemetryBootstrap(configure));
    }
}
