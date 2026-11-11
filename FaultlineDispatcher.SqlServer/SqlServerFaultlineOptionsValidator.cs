namespace FaultlineDispatcher.SqlServer;

using Microsoft.Extensions.Options;

/// <summary>Validates <see cref="SqlServerFaultlineOptions"/> on start.</summary>
internal sealed class SqlServerFaultlineOptionsValidator : IValidateOptions<SqlServerFaultlineOptions>
{
    public ValidateOptionsResult Validate(string? name, SqlServerFaultlineOptions options)
    {
        var failures = new List<string>();

        if (options.ApplicationErrorRangeStart <= 0)
        {
            failures.Add("ApplicationErrorRangeStart must be greater than zero.");
        }

        if (options.ApplicationErrorRangeEnd < options.ApplicationErrorRangeStart)
        {
            failures.Add("ApplicationErrorRangeEnd must not be less than ApplicationErrorRangeStart.");
        }

        if (string.IsNullOrWhiteSpace(options.ApplicationErrorKey))
        {
            failures.Add("ApplicationErrorKey is required.");
        }

        foreach (var (number, key) in options.Errors)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                failures.Add($"Errors entry {number} requires a response key.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
