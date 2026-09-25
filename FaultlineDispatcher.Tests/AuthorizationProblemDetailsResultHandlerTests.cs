namespace FaultlineDispatcher.Tests;

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public class AuthorizationProblemDetailsResultHandlerTests
{
    private static readonly AuthorizationPolicy EmptyPolicy = new(
        new IAuthorizationRequirement[] { new AssertionRequirement(_ => true) },
        Array.Empty<string>());

    private static AuthorizationProblemDetailsResultHandler CreateHandler(HttpContext context) =>
        new(
            context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Http.IProblemDetailsService>(),
            NullLogger<AuthorizationProblemDetailsResultHandler>.Instance);

    [Fact]
    public async Task HandleAsync_CallsNextWhenSucceeded()
    {
        var (context, _) = TestHttpContextFactory.Create();
        var handler = CreateHandler(context);
        var nextCalled = false;

        await handler.HandleAsync(_ => { nextCalled = true; return Task.CompletedTask; }, context, EmptyPolicy, PolicyAuthorizationResult.Success());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task HandleAsync_SetsUnauthorizedStatusWhenChallenged()
    {
        var (context, body) = TestHttpContextFactory.Create(services => services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, NoOpAuthenticationHandler>("Test", _ => { }));
        var handler = CreateHandler(context);

        await handler.HandleAsync(_ => Task.CompletedTask, context, EmptyPolicy, PolicyAuthorizationResult.Challenge());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var problemDetails = TestHttpContextFactory.ReadProblemDetails(body);
        Assert.NotNull(problemDetails);
        Assert.Equal("Authentication is required.", problemDetails!.Title);
    }

    [Fact]
    public async Task HandleAsync_SetsForbiddenStatusWhenForbidden()
    {
        var (context, body) = TestHttpContextFactory.Create(services => services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, NoOpAuthenticationHandler>("Test", _ => { }));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") }));
        var handler = CreateHandler(context);

        await handler.HandleAsync(_ => Task.CompletedTask, context, EmptyPolicy, PolicyAuthorizationResult.Forbid());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        var problemDetails = TestHttpContextFactory.ReadProblemDetails(body);
        Assert.NotNull(problemDetails);
        Assert.Equal("You do not have permission to perform this action.", problemDetails!.Title);
    }

    [Fact]
    public void Constructor_ThrowsWhenProblemDetailsServiceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new AuthorizationProblemDetailsResultHandler(null!, NullLogger<AuthorizationProblemDetailsResultHandler>.Instance));
    }

    [Fact]
    public void Constructor_ThrowsWhenLoggerIsNull()
    {
        var (context, _) = TestHttpContextFactory.Create();
        var problemDetailsService = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Http.IProblemDetailsService>();

        Assert.Throws<ArgumentNullException>(() => new AuthorizationProblemDetailsResultHandler(problemDetailsService, null!));
    }

    private sealed class NoOpAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public NoOpAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());

        protected override Task HandleChallengeAsync(AuthenticationProperties properties) => Task.CompletedTask;

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties) => Task.CompletedTask;
    }
}
