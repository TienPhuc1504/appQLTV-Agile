using FluentAssertions;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Infrastructure;
using LibraryManagement.Infrastructure.Data;
using LibraryManagement.Infrastructure.Repositories;
using LibraryManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Tests.Services;

public sealed class InfrastructureDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_WithValidConfiguration_ShouldRegisterAuthenticationServices()
    {
        IConfiguration configuration = CreateConfiguration("4");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        provider.GetRequiredService<IDbContextFactory<LibraryDbContext>>()
            .Should()
            .NotBeNull();
        provider.GetRequiredService<ICurrentUserService>()
            .Should()
            .BeOfType<CurrentUserService>();
        provider.GetRequiredService<IPasswordHasher>()
            .Should()
            .BeOfType<BcryptPasswordHasher>();
        provider.GetRequiredService<ILoginPreferenceService>()
            .Should()
            .BeOfType<JsonLoginPreferenceService>();
        provider.GetRequiredService<IAuthenticationService>()
            .Should()
            .BeOfType<AuthenticationService>();
        provider.GetRequiredService<ICategoryRepository>()
            .Should()
            .BeOfType<CategoryRepository>();
        provider.GetRequiredService<IAuthorRepository>()
            .Should()
            .BeOfType<AuthorRepository>();
        provider.GetRequiredService<IPublisherRepository>()
            .Should()
            .BeOfType<PublisherRepository>();
        provider.GetRequiredService<ICategoryService>()
            .Should()
            .BeOfType<CategoryService>();
        provider.GetRequiredService<IAuthorService>()
            .Should()
            .BeOfType<AuthorService>();
        provider.GetRequiredService<IPublisherService>()
            .Should()
            .BeOfType<PublisherService>();
        provider.GetRequiredService<IBookRepository>()
            .Should()
            .BeOfType<BookRepository>();
        provider.GetRequiredService<IBookCopyRepository>()
            .Should()
            .BeOfType<BookCopyRepository>();
        provider.GetRequiredService<IBookService>()
            .Should()
            .BeOfType<BookService>();
        provider.GetRequiredService<IBookCopyService>()
            .Should()
            .BeOfType<BookCopyService>();
        provider.GetRequiredService<IBookCoverStorageService>()
            .Should()
            .BeOfType<BookCoverStorageService>();
        provider.GetRequiredService<IReaderRepository>()
            .Should()
            .BeOfType<ReaderRepository>();
        provider.GetRequiredService<IReaderService>()
            .Should()
            .BeOfType<ReaderService>();
        provider.GetRequiredService<IBorrowRepository>()
            .Should()
            .BeOfType<BorrowRepository>();
        provider.GetRequiredService<IBorrowService>()
            .Should()
            .BeOfType<BorrowService>();
        provider.GetRequiredService<IReturnRepository>()
            .Should()
            .BeOfType<ReturnRepository>();
        provider.GetRequiredService<IReturnService>()
            .Should()
            .BeOfType<ReturnService>();
        provider.GetRequiredService<IFineRepository>()
            .Should()
            .BeOfType<FineRepository>();
        provider.GetRequiredService<IFineService>()
            .Should()
            .BeOfType<FineService>();
        provider.GetRequiredService<IDashboardRepository>()
            .Should()
            .BeOfType<DashboardRepository>();
        provider.GetRequiredService<IDashboardService>()
            .Should()
            .BeOfType<DashboardService>();
        provider.GetRequiredService<TimeProvider>()
            .Should()
            .BeSameAs(TimeProvider.System);
        provider.GetRequiredService<IEmployeeRepository>()
            .Should()
            .BeOfType<EmployeeRepository>();
        provider.GetRequiredService<IEmployeeService>()
            .Should()
            .BeOfType<EmployeeService>();
        provider.GetRequiredService<ISystemSettingRepository>()
            .Should()
            .BeOfType<SystemSettingRepository>();
        provider.GetRequiredService<ISystemSettingService>()
            .Should()
            .BeOfType<SystemSettingService>();
        provider.GetRequiredService<IActivityLogRepository>()
            .Should()
            .BeOfType<ActivityLogRepository>();
        provider.GetRequiredService<IActivityLogService>()
            .Should()
            .BeOfType<ActivityLogService>();
        provider.GetRequiredService<IDatabaseBackupService>()
            .Should()
            .BeOfType<DatabaseBackupService>();
    }

    [Fact]
    public void AddInfrastructure_WithInvalidBcryptWorkFactor_ShouldFailOnResolution()
    {
        IConfiguration configuration = CreateConfiguration("invalid");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Action action = () => provider.GetRequiredService<IPasswordHasher>();

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*BCryptWorkFactor*");
    }

    private static IConfiguration CreateConfiguration(string workFactor)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:LibraryDatabase"] =
                $"Data Source={Path.Combine(Path.GetTempPath(), "LibraryManagement.Tests", "di-library.db")};Foreign Keys=True",
            ["Security:BCryptWorkFactor"] = workFactor,
            ["Storage:LoginPreferencesFile"] =
                Path.Combine(
                    Path.GetTempPath(),
                    "LibraryManagement.Tests",
                    "login-preferences.json"),
            ["Storage:BookCoversDirectory"] =
                Path.Combine(
                    Path.GetTempPath(),
                    "LibraryManagement.Tests",
                    "BookCovers")
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
