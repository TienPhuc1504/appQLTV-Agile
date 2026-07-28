using System.Windows;
using LibraryManagement.App.ViewModels;
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

    public App()
    {
        var settings = new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        };
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(settings);

        builder.Logging.ClearProviders();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddDebug();

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

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

            MainWindow window = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            ILogger<App> logger = _host.Services.GetRequiredService<ILogger<App>>();
            logger.LogCritical(exception, "Không thể khởi động ứng dụng.");

            MessageBox.Show(
                "Không thể khởi tạo cơ sở dữ liệu. Vui lòng kiểm tra nhật ký ứng dụng.",
                "Lỗi khởi động",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ILogger<App> logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Ứng dụng LibraryManagement đang dừng.");

        _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        _host.Dispose();

        base.OnExit(e);
    }
}
