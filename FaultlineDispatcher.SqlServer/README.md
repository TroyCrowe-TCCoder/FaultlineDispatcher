# FaultlineDispatcher.SqlServer

SQL Server exception mapper for [FaultlineDispatcher](https://www.nuget.org/packages/FaultlineDispatcher).
Maps `SqlException` error numbers to response keys the host defines. Only the error number is used; the
SQL message, constraint, table, column and procedure names never reach the caller.

## Usage

The host supplies every value in code. There is no default range, no section name and no `IConfiguration`
binding. Options are validated when the host starts.

```csharp
builder.Services.AddFaultlineDispatcher(o => { /* define all responses */ });
builder.Services.AddFaultlineSqlServer(o =>
{
	o.ApplicationErrorRangeStart = 60000;
	o.ApplicationErrorRangeEnd = 60199;
	o.ApplicationErrorKey = FaultlineResponseKeys.BadRequest;
	o.Errors["60073"] = FaultlineResponseKeys.ContentNotFound;
});
```

| Option | Required | Meaning |
|---|---|---|
| `ApplicationErrorRangeStart` | yes | First `THROW` number treated as an application error (greater than 0) |
| `ApplicationErrorRangeEnd` | yes | Last number in the range (not less than the start) |
| `ApplicationErrorKey` | yes | Response key used for numbers in the range |
| `Errors` | no | Specific number → response key overrides |

## Built-in mappings

| SQL error | Response key |
|---|---|
| 2601, 2627 (duplicate key) | `Conflict` |
| 1205 (deadlock victim) | `Unavailable` |
| -2 (command timeout) | `DependencyTimeout` |
| 53, 233, 4060, 10928, 10929, 40197, 40501, 40613, 49918, 49919, 49920 (unreachable / unavailable / throttled) | `Unavailable` |
| Inside the application range | `ApplicationErrorKey` |

Any other number falls through to the `Unexpected` response. The status and message for every key come from
the host's `AddFaultlineDispatcher` options.

`Errors` entries win over every built-in mapping. A `THROW` number is only safe to map if it means the same
thing in every stored procedure that raises it.

## Breaking changes in 0.2.0

- `AddFaultlineSqlServer()` is now `AddFaultlineSqlServer(Action<SqlServerFaultlineOptions>)`.
- The `Faultline:SqlServer` configuration section and the option defaults are removed.
