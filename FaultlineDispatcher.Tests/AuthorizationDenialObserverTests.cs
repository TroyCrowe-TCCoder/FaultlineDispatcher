namespace FaultlineDispatcher.Tests;

using Microsoft.AspNetCore.Authorization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class AuthorizationDenialObserverTests
{
    private static readonly AuthorizationPolicy EmptyPolicy = new(
        [new Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement()],
        []);

    [Fact]
    public async Task Observer_IsNotifiedOnForbid_BeforeResponseIsWritten()
    {
        var observer = new RecordingObserver();
        var (context, _) = Create(s => s.AddSingleton<IAuthorizationDenialObserver>(observer));
        var handler = context.RequestServices.GetRequiredService<IAuthorizationMiddlewareResultHandler>();

        await handler.HandleAsync(_ => Task.CompletedTask, context, EmptyPolicy, PolicyAuthorizationResult.Forbid());

        Assert.Equal(1, observer.Calls);
        Assert.True(observer.LastResultForbidden);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task Observer_IsNotifiedOnChallenge()
    {
        var observer = new RecordingObserver();
        var (context, _) = Create(s => s.AddSingleton<IAuthorizationDenialObserver>(observer));
        var handler = context.RequestServices.GetRequiredService<IAuthorizationMiddlewareResultHandler>();

        await handler.HandleAsync(_ => Task.CompletedTask, context, EmptyPolicy, PolicyAuthorizationResult.Challenge());

        Assert.Equal(1, observer.Calls);
        Assert.False(observer.LastResultForbidden);
    }

    [Fact]
    public async Task Observer_IsNotNotifiedOnSuccess()
    {
        var observer = new RecordingObserver();
        var (context, _) = Create(s => s.AddSingleton<IAuthorizationDenialObserver>(observer));
        var handler = context.RequestServices.GetRequiredService<IAuthorizationMiddlewareResultHandler>();

        await handler.HandleAsync(_ => Task.CompletedTask, context, EmptyPolicy, PolicyAuthorizationResult.Success());

        Assert.Equal(0, observer.Calls);
    }

    [Fact]
    public async Task FailingObserver_DoesNotChangeTheResponse()
    {
        var (context, _) = Create(s => s.AddAuthorizationDenialObserver<ThrowingObserver>());
        var handler = context.RequestServices.GetRequiredService<IAuthorizationMiddlewareResultHandler>();

        await handler.HandleAsync(_ => Task.CompletedTask, context, EmptyPolicy, PolicyAuthorizationResult.Forbid());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    private static (HttpContext Context, MemoryStream Body) Create(Action<IServiceCollection> configure) =>
        TestHttpContextFactory.Create(services =>
        {
            services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, NoOpAuthenticationHandler>("Test", _ => { });
            services.AddFaultlineDispatcher();
            configure(services);
        });

    private sealed class NoOpAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
    }

    private sealed class RecordingObserver : IAuthorizationDenialObserver
    {
        public int Calls { get; private set; }

        public bool LastResultForbidden { get; private set; }

        public Task OnDeniedAsync(HttpContext context, PolicyAuthorizationResult result, CancellationToken cancellationToken)
        {
            Calls++;
            LastResultForbidden = result.Forbidden;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingObserver : IAuthorizationDenialObserver
    {
        public Task OnDeniedAsync(HttpContext context, PolicyAuthorizationResult result, CancellationToken cancellationToken)
            => throw new InvalidOperationException("observer failure");
    }
}
