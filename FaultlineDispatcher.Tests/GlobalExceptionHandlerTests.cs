namespace FaultlineDispatcher.Tests;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_SetsInternalServerErrorStatusCode()
    {
        var (context, body) = TestHttpContextFactory.Create();
        var handler = CreateHandler(context);

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WritesProblemDetailsBody()
    {
        var (context, body) = TestHttpContextFactory.Create();
        var handler = CreateHandler(context);

        await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        var problemDetails = TestHttpContextFactory.ReadProblemDetails(body);
        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails!.Status);
        Assert.Equal("An unexpected error occurred.", problemDetails.Title);
    }

    [Fact]
    public void Constructor_ThrowsWhenLoggerIsNull()
    {
        var (context, _) = TestHttpContextFactory.Create();
        var problemDetailsService = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Http.IProblemDetailsService>();

        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(null!, problemDetailsService, Options.Create(new FaultlineOptions())));
    }

    [Fact]
    public void Constructor_ThrowsWhenProblemDetailsServiceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, null!, Options.Create(new FaultlineOptions())));
    }

    [Fact]
    public void Constructor_ThrowsWhenOptionsAreNull()
    {
        var (context, _) = TestHttpContextFactory.Create();
        var problemDetailsService = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Http.IProblemDetailsService>();

        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, problemDetailsService, null!));
    }

    [Fact]
    public async Task TryHandleAsync_ThrowsWhenHttpContextIsNull()
    {
        var (context, _) = TestHttpContextFactory.Create();
        var handler = CreateHandler(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.TryHandleAsync(null!, new InvalidOperationException(), CancellationToken.None).AsTask());
    }

    private static GlobalExceptionHandler CreateHandler(HttpContext context)
    {
        var options = new FaultlineOptions();
        TestResponses.DefineAll(options);

        return new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Http.IProblemDetailsService>(),
            Options.Create(options));
    }
}
