# FaultlineDispatcher

Shared ASP.NET Core building blocks for centralized error handling and authentication/authorization
failure response shaping.

## What this package does

- `GlobalExceptionHandler` — implements `IExceptionHandler`. Maps each unhandled request exception to a
  safe, configurable response (status code, title, detail), logs it exactly once with structured
  metadata, and writes a `ProblemDetails` response that carries the request's `traceId`. Exception text
  is never returned to the caller.
- `AuthorizationProblemDetailsResultHandler` — implements `IAuthorizationMiddlewareResultHandler`.
  Shapes 401/403 authorization outcomes into `ProblemDetails` responses instead of empty status
  codes, and logs authentication-challenge/forbidden security events. Registered
  `IAuthorizationDenialObserver`s are notified first (for example, to write a security audit entry).

## Usage

```csharp
builder.Services.AddFaultlineDispatcher();
// ...
app.UseExceptionHandler();
```

`AddFaultlineDispatcher()` registers `ProblemDetails`, the exception handler, the authorization result
handler and the `Faultline` options.

## Response mapping

The status code gives the category; the message is fixed and configurable. Specifics go to the log, correlated by `traceId`.

| Response key | Default status | Default title | Used for |
|---|---|---|---|
| `BadRequest` | 400 | The request is invalid. | Malformed or invalid requests (`BadHttpRequestException`, including 413) |
| `ContentNotFound` | 400 | The requested content could not be found. | A write that targets a record that does not exist |
| `Conflict` | 409 | This request conflicts with existing data. | Duplicate or conflicting data |
| `RequestTimeout` | 408 | The request timed out. | `BadHttpRequestException` with status 408 |
| `Unavailable` | 503 | The service is temporarily unavailable. | Deadlocks, database or dependency unavailable |
| `DependencyTimeout` | 504 | The service took too long to respond. | `TimeoutException` and command timeouts |
| `Unexpected` | 500 | An unexpected error occurred. | Everything not recognized |

- A client that disconnects (`OperationCanceledException` while `RequestAborted` is set) is not logged as
  an error and gets no body (status 499).
- 404 is not used for missing records. A read that finds nothing should return `204 No Content` from the
  controller; the client decides what to show.
- Rate limiting (429) comes from the rate-limiter middleware, not from exception mapping.

### Overriding messages

Override any field of any key in configuration. Fields you leave out keep the library default:

```json
{
  "Faultline": {
	"Responses": {
	  "Conflict": { "Title": "That already exists." },
	  "Forbidden": { "Status": 403, "Title": "Access denied." }
	}
  }
}
```

A key the library does not know (such as `Forbidden` above) is defined entirely by the host. Without a
configured status it answers 500.

### Mapping your own exceptions

```csharp
public sealed class ClientAccessMapper : IExceptionMapper
{
	public string? Map(Exception exception) =>
		exception is ClientAccessDeniedException ? "Forbidden" : null;
}

builder.Services.AddExceptionMapper<ClientAccessMapper>();
```

Mappers run in registration order and before the built-in mapper. Each exception is tried first, then its
inner exceptions. Return a response key, never exception text.

### Authorization denial observers

```csharp
builder.Services.AddAuthorizationDenialObserver<SecurityAuditObserver>();
```

Observers run on every 401 and 403 before the response is written. A failing observer is logged and
never changes the response.

## Plugins

- `FaultlineDispatcher.SqlServer` — maps `SqlException` error numbers to response keys.
- `FaultlineDispatcher.Telemetry.ApplicationInsights` — Application Insights telemetry (unrelated to
  error responses).

## Design notes

This package is intentionally brand-neutral and framework-only: it has no dependency on any
specific application's domain types. It is unrelated to and does not share code with the
`AuditLoggingPipeline` package, which covers a separate concern (action/state audit logging).
