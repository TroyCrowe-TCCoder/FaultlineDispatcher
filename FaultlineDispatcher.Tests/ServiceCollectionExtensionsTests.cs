namespace FaultlineDispatcher.Tests;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFaultlineDispatcher_RegistersAuthorizationMiddlewareResultHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddFaultlineDispatcher();

        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>();
        Assert.IsType<AuthorizationProblemDetailsResultHandler>(handler);
    }

    [Fact]
    public void AddFaultlineDispatcher_ThrowsWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddFaultlineDispatcher());
    }

    [Fact]
    public void AddFaultlineDispatcher_ReturnsSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddFaultlineDispatcher();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddTelemetryBootstrap_InvokesPluginConfigure()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        FakeTelemetryBootstrap.ConfigureCallCount = 0;

        services.AddTelemetryBootstrap<FakeTelemetryBootstrap>(configuration);

        Assert.Equal(1, FakeTelemetryBootstrap.ConfigureCallCount);
    }

    [Fact]
    public void AddTelemetryBootstrap_ThrowsWhenConfigurationIsNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddTelemetryBootstrap<FakeTelemetryBootstrap>(null!));
    }

    private sealed class FakeTelemetryBootstrap : ITelemetryBootstrap
    {
        public static int ConfigureCallCount;

        public void Configure(IServiceCollection services, IConfiguration configuration)
        {
            ConfigureCallCount++;
        }
    }
}
