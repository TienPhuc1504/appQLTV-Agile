using LibraryManagement.Infrastructure.Data;
using LibraryManagement.Infrastructure.Initialization;
using LibraryManagement.Infrastructure.Repositories;
using LibraryManagement.Infrastructure.Services;
using LibraryManagement.Core.Interfaces;
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
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IPasswordHasher>(
            _ => new BcryptPasswordHasher(GetBcryptWorkFactor(configuration)));
        services.AddSingleton<ILoginPreferenceService>(
            serviceProvider => new JsonLoginPreferenceService(
                GetLoginPreferenceFilePath(configuration),
                serviceProvider.GetRequiredService<
                    Microsoft.Extensions.Logging.ILogger<JsonLoginPreferenceService>>()));
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<ICategoryRepository, CategoryRepository>();
        services.AddSingleton<IAuthorRepository, AuthorRepository>();
        services.AddSingleton<IPublisherRepository, PublisherRepository>();
        services.AddSingleton<ICategoryService, CategoryService>();
        services.AddSingleton<IAuthorService, AuthorService>();
        services.AddSingleton<IPublisherService, PublisherService>();

        return services;
    }

    private static int GetBcryptWorkFactor(IConfiguration configuration)
    {
        string? configuredValue = configuration["Security:BCryptWorkFactor"];
        if (!int.TryParse(
                configuredValue,
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out int workFactor))
        {
            throw new InvalidOperationException(
                "Cấu hình 'Security:BCryptWorkFactor' không hợp lệ.");
        }

        return workFactor;
    }

    private static string GetLoginPreferenceFilePath(IConfiguration configuration)
    {
        string configuredPath = configuration["Storage:LoginPreferencesFile"]
            ?? throw new InvalidOperationException(
                "Không tìm thấy cấu hình 'Storage:LoginPreferencesFile'.");

        return Environment.ExpandEnvironmentVariables(configuredPath);
    }
}
