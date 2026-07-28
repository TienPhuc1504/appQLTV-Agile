using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using LibraryManagement.App.Messages;
using LibraryManagement.App.Views;
using LibraryManagement.Infrastructure;
using LibraryManagement.Infrastructure.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App;

public partial class App : Application
{
    private readonly IHost _host;
    private Window? _activeWindow;
    private bool _windowTransitionPending;
    private bool _isSwitchingWindow;
    private bool _shutdownPending;

    public App()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var settings = new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        };
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(settings);

        builder.Logging.ClearProviders();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddDebug();

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddPresentation();

        _host = builder.Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            await _host.StartAsync();

            ILogger<App> logger = _host.Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Ứng dụng LibraryManagement đang khởi động.");

            IDatabaseInitializer databaseInitializer =
                _host.Services.GetRequiredService<IDatabaseInitializer>();
            await databaseInitializer.InitializeAsync();

            RegisterApplicationMessages();
            ShowLoginView();
        }
        catch (Exception exception)
        {
            ILogger<App> logger = _host.Services.GetRequiredService<ILogger<App>>();
            logger.LogCritical(exception, "Không thể khởi động ứng dụng.");

            MessageBox.Show(
                "Không thể khởi động ứng dụng. Vui lòng kiểm tra nhật ký để biết thêm chi tiết.",
                "Lỗi khởi động",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host.Services
            .GetRequiredService<IMessenger>()
            .UnregisterAll(this);

        ILogger<App> logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Ứng dụng LibraryManagement đang dừng.");

        _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        _host.Dispose();

        base.OnExit(e);
    }

    private void RegisterApplicationMessages()
    {
        IMessenger messenger = _host.Services.GetRequiredService<IMessenger>();
        messenger.Register<AuthenticationSucceededMessage>(
            this,
            static (recipient, _) =>
                ((App)recipient).QueueWindowTransition(
                    ((App)recipient).ShowMainWindow));
        messenger.Register<LogoutCompletedMessage>(
            this,
            static (recipient, _) =>
                ((App)recipient).QueueWindowTransition(
                    ((App)recipient).ShowLoginView));
    }

    private void ShowLoginView()
    {
        SwitchWindow(_host.Services.GetRequiredService<LoginView>());
    }

    private void ShowMainWindow()
    {
        SwitchWindow(_host.Services.GetRequiredService<MainWindow>());
    }

    private void SwitchWindow(Window nextWindow)
    {
        ArgumentNullException.ThrowIfNull(nextWindow);

        Window? previousWindow = _activeWindow;

        if (ReferenceEquals(previousWindow, nextWindow))
        {
            return;
        }

        _isSwitchingWindow = true;

        try
        {
            if (previousWindow is not null)
            {
                previousWindow.Closed -= OnActiveWindowClosed;
                previousWindow.Hide();
            }

            _activeWindow = nextWindow;
            MainWindow = nextWindow;
            nextWindow.Closed += OnActiveWindowClosed;
            nextWindow.Show();

            if (previousWindow is null)
            {
                _isSwitchingWindow = false;
                return;
            }

            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(
                    () => CompleteWindowTransition(previousWindow)));
        }
        catch
        {
            nextWindow.Closed -= OnActiveWindowClosed;
            _activeWindow = previousWindow;
            MainWindow = previousWindow;

            if (previousWindow is not null)
            {
                previousWindow.Closed += OnActiveWindowClosed;
                previousWindow.Show();
            }

            _isSwitchingWindow = false;
            throw;
        }
    }

    private void OnActiveWindowClosed(object? sender, EventArgs e)
    {
        bool activeWindowWasClosed = ReferenceEquals(sender, _activeWindow);

        if (sender is Window closedWindow)
        {
            closedWindow.Closed -= OnActiveWindowClosed;
        }

        if (activeWindowWasClosed)
        {
            _activeWindow = null;
        }

        if (_isSwitchingWindow
            || _windowTransitionPending
            || _shutdownPending
            || !activeWindowWasClosed
            || Dispatcher.HasShutdownStarted)
        {
            return;
        }

        RequestShutdown();
    }

    private void CompleteWindowTransition(Window previousWindow)
    {
        try
        {
            previousWindow.Close();
        }
        finally
        {
            _isSwitchingWindow = false;
        }

        if (_activeWindow is null)
        {
            RequestShutdown();
        }
    }

    private void RequestShutdown()
    {
        if (_shutdownPending || Dispatcher.HasShutdownStarted)
        {
            return;
        }

        _shutdownPending = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(
                () =>
                {
                    if (!Dispatcher.HasShutdownStarted)
                    {
                        Shutdown();
                    }
                }));
    }

    private void QueueWindowTransition(Action transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        if (_windowTransitionPending || Dispatcher.HasShutdownStarted)
        {
            return;
        }

        _windowTransitionPending = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(
                () =>
                {
                    try
                    {
                        transition();
                    }
                    catch (Exception exception)
                    {
                        ILogger<App> logger =
                            _host.Services.GetRequiredService<ILogger<App>>();
                        logger.LogCritical(
                            exception,
                            "Không thể chuyển đổi cửa sổ ứng dụng.");

                        MessageBox.Show(
                            "Không thể chuyển màn hình. Ứng dụng sẽ đóng để bảo vệ phiên làm việc.",
                            "Lỗi chuyển màn hình",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Shutdown(-1);
                    }
                    finally
                    {
                        _windowTransitionPending = false;
                    }
                }));
    }
}
