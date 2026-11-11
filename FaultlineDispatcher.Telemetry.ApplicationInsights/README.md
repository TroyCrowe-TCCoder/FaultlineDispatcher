# FaultlineDispatcher.Telemetry.ApplicationInsights

`ITelemetryBootstrap` plugin that wires up Application Insights for FaultlineDispatcher consumers.

## Usage

The host supplies the options in code. `ConnectionString` is required and is validated when the host
starts.

```csharp
builder.Services.AddFaultlineDispatcher(o => { /* define all responses */ });
builder.Services.AddFaultlineApplicationInsights(o =>
{
	o.ConnectionString = connectionString;
});
```

The options type is the Application Insights SDK's `ApplicationInsightsServiceOptions`. The SDK itself
may still read host configuration and environment variables for its own settings; this package does not.

Swap to a different vendor by referencing a different `ITelemetryBootstrap` plugin package instead —
no changes required to `FaultlineDispatcher` core or to application startup structure.

## Breaking changes in 0.2.0

- `AddTelemetryBootstrap<ApplicationInsightsTelemetryBootstrap>(configuration)` is replaced by
  `AddFaultlineApplicationInsights(Action<ApplicationInsightsServiceOptions>)`.
