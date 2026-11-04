namespace FaultlineDispatcher.SqlServer;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>DI extensions for the SQL Server mapper.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SqlServerExceptionMapper"/> with FaultlineDispatcher and binds
    /// <see cref="SqlServerFaultlineOptions"/> from <c>Faultline:SqlServer</c>. Call after
    /// <c>AddFaultlineDispatcher()</c>.
    /// </summary>
    public static IServiceCollection AddFaultlineSqlServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<SqlServerFaultlineOptions>().BindConfiguration(SqlServerFaultlineOptions.SectionName);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IExceptionMapper, SqlServerExceptionMapper>());

        return services;
    }
}
