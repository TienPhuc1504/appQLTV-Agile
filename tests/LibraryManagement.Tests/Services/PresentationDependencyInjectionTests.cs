using FluentAssertions;
using LibraryManagement.App;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Navigation;
using LibraryManagement.App.Notifications;
using LibraryManagement.App.Themes;
using LibraryManagement.App.ViewModels;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui;

namespace LibraryManagement.Tests.Services;

public sealed class PresentationDependencyInjectionTests
{
    [Fact]
    public void AddPresentation_ShouldRegisterAValidServiceGraph()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IAuthenticationService, StubAuthenticationService>();
        services.AddSingleton<ILoginPreferenceService, StubLoginPreferenceService>();
        services.AddPresentation();

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        provider.GetRequiredService<IAppNavigationService>()
            .Should()
            .BeSameAs(provider.GetRequiredService<IAppNavigationService>());
        provider.GetRequiredService<ISnackbarService>()
            .Should()
            .BeOfType<SnackbarService>();
        provider.GetRequiredService<IContentDialogService>()
            .Should()
            .BeOfType<ContentDialogService>();
        provider.GetRequiredService<IAppDialogService>()
            .Should()
            .BeOfType<AppDialogService>();
        provider.GetRequiredService<IAppNotificationService>()
            .Should()
            .BeOfType<AppNotificationService>();
        provider.GetRequiredService<IAppThemeService>()
            .Should()
            .BeOfType<AppThemeService>();
        provider.GetRequiredService<MainViewModel>()
            .Should()
            .BeSameAs(provider.GetRequiredService<MainViewModel>());
        provider.GetRequiredService<FoundationViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<FoundationViewModel>());
    }

    [Fact]
    public void AddPresentation_WhenServicesIsNull_ShouldThrow()
    {
        IServiceCollection services = null!;

        Action action = () => services.AddPresentation();

        action.Should().Throw<ArgumentNullException>();
    }

    private sealed class StubAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticationResult> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                AuthenticationResult.Failure("Không sử dụng trong kiểm thử DI."));
        }

        public void Logout()
        {
        }

        public Task<OperationResult> ChangePasswordAsync(
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public Task<OperationResult> ResetPasswordAsync(
            int employeeId,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public CurrentUser? GetCurrentUser() => null;

        public bool CheckPermission(Permission permission) => false;
    }

    private sealed class StubLoginPreferenceService : ILoginPreferenceService
    {
        public Task<string?> GetRememberedUsernameAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(null);
        }

        public Task SaveRememberedUsernameAsync(
            string? username,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
