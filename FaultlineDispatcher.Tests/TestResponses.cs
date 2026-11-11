namespace FaultlineDispatcher.Tests;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Host-style response definitions shared by the tests. The library itself ships none.</summary>
internal static class TestResponses
{
    public static void DefineAll(FaultlineOptions options)
    {
        options.Responses[FaultlineResponseKeys.BadRequest] = new ErrorResponse { Status = 400, Title = "The request is invalid." };
        options.Responses[FaultlineResponseKeys.ContentNotFound] = new ErrorResponse { Status = 404, Title = "The requested item was not found." };
        options.Responses[FaultlineResponseKeys.Conflict] = new ErrorResponse { Status = 409, Title = "This request conflicts with existing data." };
        options.Responses[FaultlineResponseKeys.RequestTimeout] = new ErrorResponse { Status = 408, Title = "The request timed out." };
        options.Responses[FaultlineResponseKeys.Unavailable] = new ErrorResponse { Status = 503, Title = "The service is temporarily unavailable." };
        options.Responses[FaultlineResponseKeys.DependencyTimeout] = new ErrorResponse { Status = 504, Title = "A dependent service timed out." };
        options.Responses[FaultlineResponseKeys.Unexpected] = new ErrorResponse { Status = 500, Title = "An unexpected error occurred.", Detail = "If this continues, contact support." };
        options.Responses[FaultlineResponseKeys.Unauthorized] = new ErrorResponse { Title = "Authentication is required.", Detail = "Sign in and try again." };
        options.Responses[FaultlineResponseKeys.Forbidden] = new ErrorResponse { Title = "You do not have permission to perform this action." };
    }

    /// <summary>Registers FaultlineDispatcher with every required response defined, then applies <paramref name="extra"/>.</summary>
    public static IServiceCollection AddTestFaultline(this IServiceCollection services, Action<FaultlineOptions>? extra = null) =>
        services.AddFaultlineDispatcher(options =>
        {
            DefineAll(options);
            extra?.Invoke(options);
        });
}
