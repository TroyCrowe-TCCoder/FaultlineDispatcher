namespace FaultlineDispatcher.Tests;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ExceptionMappingTests
{
    private const string Secret = "SECRET-INTERNAL-TEXT";

    [Fact]
    public async Task UnknownException_Returns500_WithoutExposingMessage()
    {
        var (context, body, handler) = Build();

        await handler.TryHandleAsync(context, new InvalidOperationException(Secret), CancellationToken.None);

        var problem = TestHttpContextFactory.ReadProblemDetails(body);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("An unexpected error occurred.", problem!.Title);
        Assert.Equal("If this continues, contact support.", problem.Detail);
        Assert.DoesNotContain(Secret, RawBody(body));
    }

    [Fact]
    public async Task ProblemDetails_IncludesTraceId()
    {
        var (context, body, handler) = Build();
        context.TraceIdentifier = "trace-123";

        await handler.TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        Assert.Contains("traceId", RawBody(body));
    }

    [Fact]
    public async Task BadHttpRequestException_Returns400_WithGenericMessage()
    {
        var (context, body, handler) = Build();

        await handler.TryHandleAsync(context, new BadHttpRequestException(Secret, StatusCodes.Status413PayloadTooLarge), CancellationToken.None);

        var problem = TestHttpContextFactory.ReadProblemDetails(body);
        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("The request is invalid.", problem!.Title);
        Assert.DoesNotContain(Secret, RawBody(body));
    }

    [Fact]
    public async Task BadHttpRequestException_408_ReturnsTimeoutMessage()
    {
        var (context, body, handler) = Build();

        await handler.TryHandleAsync(context, new BadHttpRequestException(Secret, StatusCodes.Status408RequestTimeout), CancellationToken.None);

        var problem = TestHttpContextFactory.ReadProblemDetails(body);
        Assert.Equal(408, context.Response.StatusCode);
        Assert.Equal("The request timed out.", problem!.Title);
        Assert.DoesNotContain(Secret, RawBody(body));
    }

    [Fact]
    public async Task TimeoutException_Returns504()
    {
        var (context, _, handler) = Build();

        await handler.TryHandleAsync(context, new TimeoutException(Secret), CancellationToken.None);

        Assert.Equal(504, context.Response.StatusCode);
    }

    [Fact]
    public async Task ClientCancellation_Returns499_WithoutBody()
    {
        var (context, body, handler) = Build();
        using var cts = new CancellationTokenSource();
        context.RequestAborted = cts.Token;
        await cts.CancelAsync();

        var handled = await handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(499, context.Response.StatusCode);
        Assert.Equal(0, body.Length);
    }

    [Fact]
    public async Task OperationCanceled_WithoutClientAbort_Returns500()
    {
        var (context, _, handler) = Build();

        await handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task HostMapper_IsUsed_AndFindsWrappedInnerException()
    {
        var (context, body, handler) = Build(extra: s => s.AddExceptionMapper<ConflictMapper>());
        var wrapped = new InvalidOperationException("outer", new ConflictException(Secret));

        await handler.TryHandleAsync(context, wrapped, CancellationToken.None);

        var problem = TestHttpContextFactory.ReadProblemDetails(body);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal("This request conflicts with existing data.", problem!.Title);
        Assert.DoesNotContain(Secret, RawBody(body));
    }

    [Fact]
    public async Task HostMapper_RunsBeforeBuiltInMapper()
    {
        var (context, _, handler) = Build(extra: s => s.AddExceptionMapper<TimeoutToConflictMapper>());

        await handler.TryHandleAsync(context, new TimeoutException(), CancellationToken.None);

        Assert.Equal(409, context.Response.StatusCode);
    }

    [Fact]
    public async Task FailingMapper_IsSkipped()
    {
        var (context, _, handler) = Build(extra: s => s.AddExceptionMapper<ThrowingMapper>());

        await handler.TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task HostResponse_IsWhatTheCallerSees()
    {
        var (context, body, handler) = Build(o => o.Responses[FaultlineResponseKeys.Conflict] = new ErrorResponse { Status = 409, Title = "Already exists." }, s => s.AddExceptionMapper<ConflictMapper>());

        await handler.TryHandleAsync(context, new ConflictException(Secret), CancellationToken.None);

        var problem = TestHttpContextFactory.ReadProblemDetails(body);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal("Already exists.", problem!.Title);
    }

    [Fact]
    public async Task HostResponse_CanUseAnyErrorStatus()
    {
        var (context, _, handler) = Build(o => o.Responses[FaultlineResponseKeys.Conflict] = new ErrorResponse { Status = 422, Title = "Cannot process." }, s => s.AddExceptionMapper<ConflictMapper>());

        await handler.TryHandleAsync(context, new ConflictException(Secret), CancellationToken.None);

        Assert.Equal(422, context.Response.StatusCode);
    }

    [Fact]
    public async Task UnvalidatedResponseWithInvalidStatus_FallsBackToUnexpected()
    {
        var options = new FaultlineOptions();
        TestResponses.DefineAll(options);
        options.Responses[FaultlineResponseKeys.Conflict] = new ErrorResponse { Status = 200, Title = "Fine." };
        var (context, body, handler) = BuildUnvalidated(options);

        await handler.TryHandleAsync(context, new ConflictException(Secret), CancellationToken.None);

        var problem = TestHttpContextFactory.ReadProblemDetails(body);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("An unexpected error occurred.", problem!.Title);
    }

    [Fact]
    public async Task HostDefinedKey_UsesSuppliedResponse()
    {
        var (context, body, handler) = Build(o => o.Responses["Teapot"] = new ErrorResponse { Status = 418, Title = "Short and stout." }, s => s.AddExceptionMapper<TeapotMapper>());

        await handler.TryHandleAsync(context, new ConflictException(Secret), CancellationToken.None);

        var problem = TestHttpContextFactory.ReadProblemDetails(body);
        Assert.Equal(418, context.Response.StatusCode);
        Assert.Equal("Short and stout.", problem!.Title);
        Assert.Null(problem.Detail);
    }

    [Fact]
    public async Task MapperReturningForbidden_UsesFixed403_WithHostText()
    {
        var (context, body, handler) = Build(extra: s => s.AddExceptionMapper<ForbiddenMapper>());

        await handler.TryHandleAsync(context, new ConflictException(Secret), CancellationToken.None);

        var problem = TestHttpContextFactory.ReadProblemDetails(body);
        Assert.Equal(403, context.Response.StatusCode);
        Assert.Equal("You do not have permission to perform this action.", problem!.Title);
        Assert.DoesNotContain(Secret, RawBody(body));
    }

    [Fact]
    public async Task HostDefinedKey_WithoutDefinition_Returns500()
    {
        var (context, body, handler) = Build(extra: s => s.AddExceptionMapper<TeapotMapper>());

        await handler.TryHandleAsync(context, new ConflictException(Secret), CancellationToken.None);

        Assert.Equal(500, context.Response.StatusCode);
        Assert.DoesNotContain(Secret, RawBody(body));
    }

    [Fact]
    public async Task UnvalidatedEmptyOptions_Returns500_WithoutThrowingOrExposingMessage()
    {
        var (context, body, handler) = BuildUnvalidated(new FaultlineOptions());

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException(Secret), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.DoesNotContain(Secret, RawBody(body));
    }

    private static (HttpContext Context, MemoryStream Body, IExceptionHandler Handler) Build(
        Action<FaultlineOptions>? configure = null,
        Action<IServiceCollection>? extra = null)
    {
        var (context, body) = TestHttpContextFactory.Create(services =>
        {
            services.AddTestFaultline(configure);
            extra?.Invoke(services);
        });

        return (context, body, context.RequestServices.GetRequiredService<IExceptionHandler>());
    }

    private static (HttpContext Context, MemoryStream Body, IExceptionHandler Handler) BuildUnvalidated(FaultlineOptions options)
    {
        var (context, body) = TestHttpContextFactory.Create();
        var handler = new GlobalExceptionHandler(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GlobalExceptionHandler>.Instance,
            context.RequestServices.GetRequiredService<IProblemDetailsService>(),
            Microsoft.Extensions.Options.Options.Create(options),
            [new ConflictMapper()]);

        return (context, body, handler);
    }

    private static string RawBody(MemoryStream body)
    {
        body.Position = 0;
        return new StreamReader(body, leaveOpen: true).ReadToEnd();
    }

    private sealed class ConflictException(string message) : Exception(message);

    private sealed class ConflictMapper : IExceptionMapper
    {
        public string? Map(Exception exception) => exception is ConflictException ? FaultlineResponseKeys.Conflict : null;
    }

    private sealed class TimeoutToConflictMapper : IExceptionMapper
    {
        public string? Map(Exception exception) => exception is TimeoutException ? FaultlineResponseKeys.Conflict : null;
    }

    private sealed class ForbiddenMapper : IExceptionMapper
    {
        public string? Map(Exception exception) => exception is ConflictException ? FaultlineResponseKeys.Forbidden : null;
    }

    private sealed class TeapotMapper : IExceptionMapper
    {
        public string? Map(Exception exception) => exception is ConflictException ? "Teapot" : null;
    }

    private sealed class ThrowingMapper : IExceptionMapper
    {
        public string? Map(Exception exception) => throw new InvalidOperationException("mapper failure");
    }
}
