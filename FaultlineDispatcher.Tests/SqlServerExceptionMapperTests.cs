namespace FaultlineDispatcher.Tests;

using FaultlineDispatcher.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

public class SqlServerExceptionMapperTests
{
    [Theory]
    [InlineData(2601, "Conflict")]
    [InlineData(2627, "Conflict")]
    [InlineData(1205, "Unavailable")]
    [InlineData(-2, "DependencyTimeout")]
    [InlineData(40613, "Unavailable")]
    [InlineData(4060, "Unavailable")]
    [InlineData(60000, "BadRequest")]
    [InlineData(60073, "BadRequest")]
    [InlineData(60199, "BadRequest")]
    public void ResolveKey_MapsBuiltInNumbers(int number, string expected)
    {
        Assert.Equal(expected, CreateMapper().ResolveKey(number));
    }

    [Theory]
    [InlineData(547)]
    [InlineData(208)]
    [InlineData(59999)]
    [InlineData(60200)]
    public void ResolveKey_ReturnsNullForUnmappedNumbers(int number)
    {
        Assert.Null(CreateMapper().ResolveKey(number));
    }

    [Fact]
    public void ResolveKey_ConfiguredNumberOverridesBuiltInAndRange()
    {
        var mapper = CreateMapper(o =>
        {
            o.Errors[60073] = FaultlineResponseKeys.ContentNotFound;
            o.Errors[2627] = "Custom";
        });

        Assert.Equal(FaultlineResponseKeys.ContentNotFound, mapper.ResolveKey(60073));
        Assert.Equal("Custom", mapper.ResolveKey(2627));
    }

    [Fact]
    public void ResolveKey_HonorsConfiguredRangeAndKey()
    {
        var mapper = CreateMapper(o =>
        {
            o.ApplicationErrorRangeStart = 50000;
            o.ApplicationErrorRangeEnd = 50010;
            o.ApplicationErrorKey = FaultlineResponseKeys.Conflict;
        });

        Assert.Equal(FaultlineResponseKeys.Conflict, mapper.ResolveKey(50005));
        Assert.Null(mapper.ResolveKey(60000));
    }

    [Fact]
    public void Map_ReturnsNullForNonSqlExceptions()
    {
        Assert.Null(CreateMapper().Map(new InvalidOperationException()));
    }

    [Fact]
    public void AddFaultlineSqlServer_RegistersMapperOnce()
    {
        var services = new ServiceCollection();

        services.AddFaultlineSqlServer(Configure);
        services.AddFaultlineSqlServer(Configure);

        Assert.Single(services, d => d.ServiceType == typeof(IExceptionMapper) && d.ImplementationType == typeof(SqlServerExceptionMapper));
    }

    [Fact]
    public void AddFaultlineSqlServer_ThrowsWhenConfigureIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddFaultlineSqlServer(null!));
    }

    [Fact]
    public void AddFaultlineSqlServer_WithInvalidOptions_FailsValidation()
    {
        var services = new ServiceCollection();
        services.AddFaultlineSqlServer(_ => { });

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SqlServerFaultlineOptions>>().Value);
    }

    [Fact]
    public void Validator_Succeeds_ForCompleteOptions()
    {
        var options = new SqlServerFaultlineOptions();
        Configure(options);

        Assert.True(new SqlServerFaultlineOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validator_Fails_WhenRangeEndIsBeforeStart()
    {
        var options = new SqlServerFaultlineOptions();
        Configure(options);
        options.ApplicationErrorRangeEnd = options.ApplicationErrorRangeStart - 1;

        Assert.True(new SqlServerFaultlineOptionsValidator().Validate(null, options).Failed);
    }

    [Fact]
    public void Validator_Fails_WhenApplicationErrorKeyIsBlank()
    {
        var options = new SqlServerFaultlineOptions();
        Configure(options);
        options.ApplicationErrorKey = " ";

        Assert.True(new SqlServerFaultlineOptionsValidator().Validate(null, options).Failed);
    }

    [Fact]
    public void Validator_Fails_WhenAnErrorEntryHasABlankKey()
    {
        var options = new SqlServerFaultlineOptions();
        Configure(options);
        options.Errors[60073] = string.Empty;

        Assert.True(new SqlServerFaultlineOptionsValidator().Validate(null, options).Failed);
    }

    private static void Configure(SqlServerFaultlineOptions options)
    {
        options.ApplicationErrorRangeStart = 60000;
        options.ApplicationErrorRangeEnd = 60199;
        options.ApplicationErrorKey = FaultlineResponseKeys.BadRequest;
    }

    private static SqlServerExceptionMapper CreateMapper(Action<SqlServerFaultlineOptions>? extra = null)
    {
        var options = new SqlServerFaultlineOptions();
        Configure(options);
        extra?.Invoke(options);

        return new SqlServerExceptionMapper(Options.Create(options));
    }
}
