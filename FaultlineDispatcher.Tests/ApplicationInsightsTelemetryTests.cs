namespace FaultlineDispatcher.Tests;

using FaultlineDispatcher.Telemetry.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

public class ApplicationInsightsTelemetryTests
{
    private const string ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000";

    [Fact]
    public void Validator_Succeeds_WhenConnectionStringIsSupplied()
    {
        var options = new ApplicationInsightsServiceOptions { ConnectionString = ConnectionString };

        Assert.True(new ApplicationInsightsOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validator_Fails_WhenConnectionStringIsMissing()
    {
        Assert.True(new ApplicationInsightsOptionsValidator().Validate(null, new ApplicationInsightsServiceOptions()).Failed);
    }

    [Fact]
    public void AddFaultlineApplicationInsights_AppliesHostSuppliedOptions()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddFaultlineApplicationInsights(o => o.ConnectionString = ConnectionString);

        using var app = builder.Build();
        Assert.Equal(ConnectionString, app.Services.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value.ConnectionString);
    }

    [Fact]
    public void AddFaultlineApplicationInsights_WithoutConnectionString_FailsValidation()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddFaultlineApplicationInsights(o => o.ConnectionString = null);

        Assert.Throws<OptionsValidationException>(() =>
        {
            using var app = builder.Build();
            _ = app.Services.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
        });
    }

    [Fact]
    public void Bootstrap_ThrowsWhenConfigureIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ApplicationInsightsTelemetryBootstrap(null!));
    }
}
