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
                "Data Source=:memory:;Foreign Keys=True",
            ["Security:BCryptWorkFactor"] = workFactor,
            ["Storage:LoginPreferencesFile"] =
                Path.Combine(
                    Path.GetTempPath(),
                    "LibraryManagement.Tests",
                    "login-preferences.json")
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
