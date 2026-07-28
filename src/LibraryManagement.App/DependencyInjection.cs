using CommunityToolkit.Mvvm.Messaging;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Navigation;
using LibraryManagement.App.Notifications;
using LibraryManagement.App.Themes;
using LibraryManagement.App.ViewModels;
using LibraryManagement.App.Views;
using LibraryManagement.App.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui;

namespace LibraryManagement.App;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAppNavigationService, AppNavigationService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddSingleton<IContentDialogService, ContentDialogService>();
        services.AddSingleton<IAppDialogService, AppDialogService>();
        services.AddSingleton<IAppNotificationService, AppNotificationService>();
        services.AddSingleton<IAppThemeService, AppThemeService>();
        services.AddSingleton<IMessenger, WeakReferenceMessenger>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<FoundationViewModel>();

        services.AddTransient<LoginView>();
        services.AddTransient<FoundationPage>();
        services.AddTransient<MainWindow>();

        return services;
    }
}
