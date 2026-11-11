# FaultlineDispatcher

Shared ASP.NET Core building blocks for centralized error handling and authentication/authorization
failure response shaping.

## What this package does

- `GlobalExceptionHandler` — implements `IExceptionHandler`. Maps each unhandled request exception to a
  response the host defined (status code, title, detail), logs it exactly once with structured
  metadata, and writes a `ProblemDetails` response that carries the request's `traceId`. Exception text
  is never returned to the caller.
- `AuthorizationProblemDetailsResultHandler` — implements `IAuthorizationMiddlewareResultHandler`.
  Shapes 401/403 authorization outcomes into `ProblemDetails` responses instead of empty status
  codes, and logs authentication-challenge/forbidden security events. Registered
  `IAuthorizationDenialObserver`s are notified first (for example, to write a security audit entry).

## Configuration

The library defines the options; the host supplies every value in code. The library has no defaults, no
configuration section name and does not read `IConfiguration`. Options are validated when the host starts,
so a missing response stops startup instead of failing on the first error.

```csharp
builder.Services.AddFaultlineDispatcher(o =>
{
	o.Responses[FaultlineResponseKeys.BadRequest] = new ErrorResponse { Status = 400, Title = "The request is invalid." };
	o.Responses[FaultlineResponseKeys.ContentNotFound] = new ErrorResponse { Status = 400, Title = "The requested content could not be found." };
	o.Responses[FaultlineResponseKeys.Conflict] = new ErrorResponse { Status = 409, Title = "This request conflicts with existing data." };
	o.Responses[FaultlineResponseKeys.RequestTimeout] = new ErrorResponse { Status = 408, Title = "The request timed out." };
	o.Responses[FaultlineResponseKeys.Unavailable] = new ErrorResponse { Status = 503, Title = "The service is temporarily unavailable." };
	o.Responses[FaultlineResponseKeys.DependencyTimeout] = new ErrorResponse { Status = 504, Title = "The service took too long to respond." };
	o.Responses[FaultlineResponseKeys.Unexpected] = new ErrorResponse { Status = 500, Title = "An unexpected error occurred." };
	o.Responses[FaultlineResponseKeys.Unauthorized] = new ErrorResponse { Status = 401, Title = "Authentication is required." };
	o.Responses[FaultlineResponseKeys.Forbidden] = new ErrorResponse { Status = 403, Title = "You do not have access to this resource." };
});
// ...
app.UseExceptionHandler();
```

`AddFaultlineDispatcher` registers `ProblemDetails`, the exception handler, the authorization result
handler and the options validation.

## Response mapping

The status code gives the category; the message is yours. Specifics go to the log, correlated by `traceId`.
Write messages a user can act on, and keep anything sensitive (names, identifiers, internals) out of them.
The response text is exactly what the caller sees.

| Response key | Required | Used for |
|---|---|---|
| `BadRequest` | yes | Malformed or invalid requests (`BadHttpRequestException`, including 413) |
| `ContentNotFound` | yes | A write that targets a record that does not exist |
| `Conflict` | yes | Duplicate or conflicting data |
| `RequestTimeout` | yes | `BadHttpRequestException` with status 408 |
| `Unavailable` | yes | Deadlocks, database or dependency unavailable |
| `DependencyTimeout` | yes | `TimeoutException` and command timeouts |
| `Unexpected` | yes | Everything not recognized; also the fallback for an unusable response |
| `Unauthorized` | yes | 401 outcomes. The status is fixed at 401 |
| `Forbidden` | yes | 403 outcomes. The status is fixed at 403 |

Startup fails when a required key is missing, a title is blank, a status is outside 400–599, or
`Unauthorized`/`Forbidden` carries a different status.

- A client that disconnects (`OperationCanceledException` while `RequestAborted` is set) is not logged as
  an error and gets no body (status 499).
- 404 is not used for missing records. A read that finds nothing should return `204 No Content` from the
  controller; the client decides what to show.
- Rate limiting (429) comes from the rate-limiter middleware, not from exception mapping.

### Your own keys

A key the library does not know is defined entirely by the host and used by your own mappers:

```csharp
builder.Services.AddFaultlineDispatcher(o =>
{
	// ...required keys...
	o.Responses["SeatLimitReached"] = new ErrorResponse { Status = 422, Title = "No seats are available." };
});
```

### Mapping your own exceptions

```csharp
public sealed class SeatLimitMapper : IExceptionMapper
{
	public string? Map(Exception exception) =>
		exception is SeatLimitException ? "SeatLimitReached" : null;
}

builder.Services.AddExceptionMapper<SeatLimitMapper>();
```

Mappers run in registration order and before the built-in mapper. Each exception is tried first, then its
inner exceptions. Return a response key, never exception text. A key with no usable host definition falls
back to your `Unexpected` response.

### Authorization denial observers

```csharp
builder.Services.AddAuthorizationDenialObserver<SecurityAuditObserver>();
```

Observers run on every 401 and 403 before the response is written. A failing observer is logged and
never changes the response.

### Telemetry

```csharp
builder.Services.AddTelemetryBootstrap(new MyTelemetryBootstrap());
```

`ITelemetryBootstrap.Configure(IServiceCollection)` receives no configuration. Plugin packages expose
their own `Add...` method that takes host-supplied options.

## Plugins

- `FaultlineDispatcher.SqlServer` — maps `SqlException` error numbers to response keys.
- `FaultlineDispatcher.Telemetry.ApplicationInsights` — Application Insights telemetry (unrelated to
  error responses).

## Breaking changes in 0.3.0

- `AddFaultlineDispatcher()` is now `AddFaultlineDispatcher(Action<FaultlineOptions>)`. All nine
  responses must be defined; there are no defaults.
- The `Faultline` configuration section and `FaultlineOptions.SectionName` are removed.
- `AddTelemetryBootstrap<T>(IConfiguration)` is replaced by `AddTelemetryBootstrap(ITelemetryBootstrap)`,
  and `ITelemetryBootstrap.Configure` takes only `IServiceCollection`.
- `Unauthorized` and `Forbidden` are now required response keys.
- The `GlobalExceptionHandler` and `AuthorizationProblemDetailsResultHandler` constructors take
  `IOptions<FaultlineOptions>`.

## Design notes

This package is intentionally brand-neutral and framework-only: it has no dependency on any
specific application's domain types. It is unrelated to and does not share code with the
`AuditLoggingPipeline` package, which covers a separate concern (action/state audit logging).
