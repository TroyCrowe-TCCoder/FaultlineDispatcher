# FaultlineDispatcher.Telemetry.ApplicationInsights

`ITelemetryBootstrap` plugin that wires up Application Insights for FaultlineDispatcher consumers.

## Usage

```csharp
builder.Services.AddFaultlineDispatcher();
builder.Services.AddTelemetryBootstrap<ApplicationInsightsTelemetryBootstrap>(builder.Configuration);
```

Swap to a different vendor by referencing a different `ITelemetryBootstrap` plugin package instead —
no changes required to `FaultlineDispatcher` core or to application startup structure.
