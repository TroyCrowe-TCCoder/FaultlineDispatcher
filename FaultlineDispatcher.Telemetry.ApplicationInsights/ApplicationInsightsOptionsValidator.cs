namespace FaultlineDispatcher.Telemetry.ApplicationInsights;

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.Options;

/// <summary>Validates the Application Insights SDK options on start.</summary>
internal sealed class ApplicationInsightsOptionsValidator : IValidateOptions<ApplicationInsightsServiceOptions>
{
    public ValidateOptionsResult Validate(string? name, ApplicationInsightsServiceOptions options)
    {
        return string.IsNullOrWhiteSpace(options.ConnectionString)
            ? ValidateOptionsResult.Fail("ConnectionString is required.")
            : ValidateOptionsResult.Success;
    }
}
