# FaultlineDispatcher

Shared ASP.NET Core building blocks for centralized error handling and authentication/authorization
failure response shaping.

## What this package does

- `GlobalExceptionHandler` — implements `IExceptionHandler`. Logs every unhandled request
  exception exactly once with structured metadata, then writes a `ProblemDetails` response.
- `AuthorizationProblemDetailsResultHandler` — implements `IAuthorizationMiddlewareResultHandler`.
  Shapes 401/403 authorization outcomes into `ProblemDetails` responses instead of empty status
  codes, and logs authentication-challenge/forbidden security events.

## Usage

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationProblemDetailsResultHandler>();
// ...
app.UseExceptionHandler();
```

## Design notes

This package is intentionally brand-neutral and framework-only: it has no dependency on any
specific application's domain types. It is unrelated to and does not share code with the
`AuditLoggingPipeline` package, which covers a separate concern (action/state audit logging).
for the non-blocking audit/state-trail interception pattern.
