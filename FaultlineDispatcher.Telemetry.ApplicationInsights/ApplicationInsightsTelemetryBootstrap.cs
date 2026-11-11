namespace FaultlineDispatcher.Telemetry.ApplicationInsights;

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
/// Registers Application Insights telemetry. The host supplies the SDK options in code, so the library holds no
/// configuration of its own. The options are validated on start: a connection string is required.
/// </summary>
public sealed class ApplicationInsightsTelemetryBootstrap : ITelemetryBootstrap
{
    private readonly Action<ApplicationInsightsServiceOptions> _configure;

    /// <summary>Creates the bootstrap with the host-supplied SDK options delegate.</summary>
    public ApplicationInsightsTelemetryBootstrap(Action<ApplicationInsightsServiceOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _configure = configure;
    }

    /// <inheritdoc />
    public void Configure(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApplicationInsightsTelemetry(_configure);
        services.AddOptions<ApplicationInsightsServiceOptions>().ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ApplicationInsightsServiceOptions>, ApplicationInsightsOptionsValidator>());
    }
}
