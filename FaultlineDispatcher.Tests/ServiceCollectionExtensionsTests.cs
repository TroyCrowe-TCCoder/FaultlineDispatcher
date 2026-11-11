namespace FaultlineDispatcher.Tests;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFaultlineDispatcher_RegistersAuthorizationMiddlewareResultHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddTestFaultline();

        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>();
        Assert.IsType<AuthorizationProblemDetailsResultHandler>(handler);
    }

    [Fact]
    public void AddFaultlineDispatcher_ThrowsWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddFaultlineDispatcher(_ => { }));
    }

    [Fact]
    public void AddFaultlineDispatcher_ThrowsWhenConfigureIsNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddFaultlineDispatcher(null!));
    }

    [Fact]
    public void AddFaultlineDispatcher_ReturnsSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddTestFaultline();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddFaultlineDispatcher_AppliesHostSuppliedResponses()
    {
        var services = new ServiceCollection();
        services.AddTestFaultline();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<FaultlineOptions>>().Value;

        Assert.Equal("The request is invalid.", options.Responses[FaultlineResponseKeys.BadRequest].Title);
    }

    [Fact]
    public void AddFaultlineDispatcher_WithMissingResponses_FailsValidation()
    {
        var services = new ServiceCollection();
        services.AddFaultlineDispatcher(_ => { });

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<FaultlineOptions>>().Value);
    }

    [Fact]
    public void AddTelemetryBootstrap_InvokesPluginConfigure()
    {
        var services = new ServiceCollection();
        var bootstrap = new FakeTelemetryBootstrap();

        services.AddTelemetryBootstrap(bootstrap);

        Assert.Equal(1, bootstrap.ConfigureCallCount);
    }

    [Fact]
    public void AddTelemetryBootstrap_ThrowsWhenBootstrapIsNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddTelemetryBootstrap(null!));
    }

    private sealed class FakeTelemetryBootstrap : ITelemetryBootstrap
    {
        public int ConfigureCallCount { get; private set; }

        public void Configure(IServiceCollection services)
        {
            ConfigureCallCount++;
        }
    }
}
