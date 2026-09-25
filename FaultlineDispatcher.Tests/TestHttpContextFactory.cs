namespace FaultlineDispatcher.Tests;

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Shared helpers for building a minimal but real DI-backed <see cref="HttpContext"/> so tests
/// exercise the actual <see cref="IProblemDetailsService"/> pipeline instead of mocking it away.
/// </summary>
internal static class TestHttpContextFactory
{
    public static (HttpContext Context, MemoryStream Body) Create(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddProblemDetails();
        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider();

        var body = new MemoryStream();
        var context = new DefaultHttpContext
        {
            RequestServices = provider,
        };
        context.Response.Body = body;

        return (context, body);
    }

    public static ProblemDetails? ReadProblemDetails(MemoryStream body)
    {
        body.Position = 0;
        if (body.Length == 0)
        {
            return null;
        }

        using var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<ProblemDetails>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}

/// <summary>
/// A no-op <see cref="ILoggerProvider"/> so tests don't depend on console/debug logging sinks.
/// </summary>
internal sealed class NullLoggerProvider : ILoggerProvider
{
    public static readonly NullLoggerProvider Instance = new();

    public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

    public void Dispose()
    {
    }

    private sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}
