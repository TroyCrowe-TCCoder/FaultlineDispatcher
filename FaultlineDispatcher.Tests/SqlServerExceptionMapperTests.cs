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
        Assert.Equal(expected, new SqlServerExceptionMapper().ResolveKey(number));
    }

    [Theory]
    [InlineData(547)]
    [InlineData(208)]
    [InlineData(59999)]
    [InlineData(60200)]
    public void ResolveKey_ReturnsNullForUnmappedNumbers(int number)
    {
        Assert.Null(new SqlServerExceptionMapper().ResolveKey(number));
    }

    [Fact]
    public void ResolveKey_ConfiguredNumberOverridesBuiltInAndRange()
    {
        var options = new SqlServerFaultlineOptions();
        options.Errors[60073] = FaultlineResponseKeys.ContentNotFound;
        options.Errors[2627] = "Custom";
        var mapper = new SqlServerExceptionMapper(Options.Create(options));

        Assert.Equal(FaultlineResponseKeys.ContentNotFound, mapper.ResolveKey(60073));
        Assert.Equal("Custom", mapper.ResolveKey(2627));
    }

    [Fact]
    public void ResolveKey_HonorsConfiguredRangeAndKey()
    {
        var options = new SqlServerFaultlineOptions
        {
            ApplicationErrorRangeStart = 50000,
            ApplicationErrorRangeEnd = 50010,
            ApplicationErrorKey = FaultlineResponseKeys.Conflict
        };
        var mapper = new SqlServerExceptionMapper(Options.Create(options));

        Assert.Equal(FaultlineResponseKeys.Conflict, mapper.ResolveKey(50005));
        Assert.Null(mapper.ResolveKey(60000));
    }

    [Fact]
    public void Map_ReturnsNullForNonSqlExceptions()
    {
        Assert.Null(new SqlServerExceptionMapper().Map(new InvalidOperationException()));
    }

    [Fact]
    public void AddFaultlineSqlServer_RegistersMapperOnce()
    {
        var services = new ServiceCollection();

        services.AddFaultlineSqlServer();
        services.AddFaultlineSqlServer();

        Assert.Single(services, d => d.ServiceType == typeof(IExceptionMapper) && d.ImplementationType == typeof(SqlServerExceptionMapper));
    }
}
