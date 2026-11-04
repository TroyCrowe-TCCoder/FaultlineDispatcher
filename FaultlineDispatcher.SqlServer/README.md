# FaultlineDispatcher.SqlServer

SQL Server exception mapper for [FaultlineDispatcher](https://www.nuget.org/packages/FaultlineDispatcher).
Maps `SqlException` error numbers to safe, configurable HTTP responses. Only the error number is used; the
SQL message, constraint, table, column and procedure names never reach the caller.

## Usage

```csharp
builder.Services.AddFaultlineDispatcher();
builder.Services.AddFaultlineSqlServer();
```

## Built-in mappings

| SQL error | Response key | Default status |
|---|---|---|
| 2601, 2627 (duplicate key) | `Conflict` | 409 |
| 1205 (deadlock victim) | `Unavailable` | 503 |
| -2 (command timeout) | `DependencyTimeout` | 504 |
| 53, 233, 4060, 10928, 10929, 40197, 40501, 40613, 49918, 49919, 49920 (unreachable / unavailable / throttled) | `Unavailable` | 503 |
| 60000–60199 (application `THROW`) | `BadRequest` | 400 |

Any other number falls through to the `Unexpected` (500) response.

## Configuration

Map your own `THROW` numbers, or change the application range, under `Faultline:SqlServer`:

```json
{
  "Faultline": {
	"SqlServer": {
	  "ApplicationErrorRangeStart": 60000,
	  "ApplicationErrorRangeEnd": 60199,
	  "ApplicationErrorKey": "BadRequest",
	  "Errors": {
		"60073": "ContentNotFound"
	  }
	}
  }
}
```

`Errors` entries win over every built-in mapping. A `THROW` number is only safe to map if it means the same
thing in every stored procedure that raises it.
