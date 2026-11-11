namespace FaultlineDispatcher.Tests;

using Microsoft.Extensions.Options;
using Xunit;

public class FaultlineOptionsValidatorTests
{
    private static readonly FaultlineOptionsValidator Validator = new();

    [Fact]
    public void Validate_Succeeds_WhenAllRequiredResponsesAreDefined()
    {
        var options = new FaultlineOptions();
        TestResponses.DefineAll(options);

        Assert.True(Validator.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_Fails_WhenNothingIsDefined()
    {
        var result = Validator.Validate(null, new FaultlineOptions());

        Assert.True(result.Failed);
        Assert.Equal(ErrorResponseCatalog.RequiredKeys.Count, result.Failures!.Count());
    }

    [Theory]
    [InlineData(FaultlineResponseKeys.Conflict)]
    [InlineData(FaultlineResponseKeys.Unexpected)]
    [InlineData(FaultlineResponseKeys.Forbidden)]
    public void Validate_Fails_WhenARequiredKeyIsMissing(string key)
    {
        var options = new FaultlineOptions();
        TestResponses.DefineAll(options);
        options.Responses.Remove(key);

        var result = Validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains(key));
    }

    [Fact]
    public void Validate_Fails_WhenTitleIsBlank()
    {
        var options = new FaultlineOptions();
        TestResponses.DefineAll(options);
        options.Responses[FaultlineResponseKeys.Conflict] = new ErrorResponse { Status = 409, Title = " " };

        Assert.True(Validator.Validate(null, options).Failed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(200)]
    [InlineData(399)]
    [InlineData(600)]
    public void Validate_Fails_WhenStatusIsNotAnErrorStatus(int? status)
    {
        var options = new FaultlineOptions();
        TestResponses.DefineAll(options);
        options.Responses[FaultlineResponseKeys.Conflict] = new ErrorResponse { Status = status, Title = "Conflict." };

        Assert.True(Validator.Validate(null, options).Failed);
    }

    [Fact]
    public void Validate_Fails_WhenFixedStatusKeyHasADifferentStatus()
    {
        var options = new FaultlineOptions();
        TestResponses.DefineAll(options);
        options.Responses[FaultlineResponseKeys.Forbidden] = new ErrorResponse { Status = 404, Title = "No." };

        Assert.True(Validator.Validate(null, options).Failed);
    }

    [Fact]
    public void Validate_Succeeds_WhenFixedStatusKeyRepeatsItsOwnStatus()
    {
        var options = new FaultlineOptions();
        TestResponses.DefineAll(options);
        options.Responses[FaultlineResponseKeys.Forbidden] = new ErrorResponse { Status = 403, Title = "No." };

        Assert.True(Validator.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_Fails_WhenAnEntryIsNull()
    {
        var options = new FaultlineOptions();
        TestResponses.DefineAll(options);
        options.Responses[FaultlineResponseKeys.Conflict] = null!;

        Assert.True(Validator.Validate(null, options).Failed);
    }
}
