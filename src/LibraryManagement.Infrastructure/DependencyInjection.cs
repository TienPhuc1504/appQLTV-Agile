using LibraryManagement.Infrastructure.Data;
using LibraryManagement.Infrastructure.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = SqliteConnectionStringResolver.Resolve(
            configuration,
            AppContext.BaseDirectory);

        SQLitePCL.Batteries_V2.Init();

        services.AddDbContextFactory<LibraryDbContext>(options =>
            options
                .UseSqlite(connectionString)
                .EnableDetailedErrors());
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }
}
