namespace FaultlineDispatcher;

using Microsoft.Extensions.Options;

/// <summary>Validates <see cref="FaultlineOptions"/> on start so a misconfigured host cannot return blank or wrong errors.</summary>
internal sealed class FaultlineOptionsValidator : IValidateOptions<FaultlineOptions>
{
    public ValidateOptionsResult Validate(string? name, FaultlineOptions options)
    {
        var failures = new List<string>();

        foreach (var key in ErrorResponseCatalog.RequiredKeys)
        {
            if (!options.Responses.ContainsKey(key))
            {
                failures.Add($"Faultline response '{key}' is not defined. Define it in AddFaultlineDispatcher.");
            }
        }

        foreach (var (key, response) in options.Responses)
        {
            if (response is null)
            {
                failures.Add($"Faultline response '{key}' is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(response.Title))
            {
                failures.Add($"Faultline response '{key}' requires a Title.");
            }

            if (ErrorResponseCatalog.FixedStatusKeys.TryGetValue(key, out var fixedStatus))
            {
                if (response.Status is not null && response.Status != fixedStatus)
                {
                    failures.Add($"Faultline response '{key}' has a fixed status of {fixedStatus}; remove Status or set it to {fixedStatus}.");
                }
            }
            else if (response.Status is not (>= 400 and <= 599))
            {
                failures.Add($"Faultline response '{key}' requires a Status between 400 and 599.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
