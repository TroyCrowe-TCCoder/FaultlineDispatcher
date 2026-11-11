namespace FaultlineDispatcher.SqlServer;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>DI extensions for the SQL Server mapper.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SqlServerExceptionMapper"/> with FaultlineDispatcher. The host supplies the options in
    /// <paramref name="configure"/>, which are validated on start. Call after <c>AddFaultlineDispatcher()</c>.
    /// </summary>
    public static IServiceCollection AddFaultlineSqlServer(this IServiceCollection services, Action<SqlServerFaultlineOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<SqlServerFaultlineOptions>().Configure(configure).ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SqlServerFaultlineOptions>, SqlServerFaultlineOptionsValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IExceptionMapper, SqlServerExceptionMapper>());

        return services;
    }
}
