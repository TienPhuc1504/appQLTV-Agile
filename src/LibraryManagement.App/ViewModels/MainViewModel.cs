using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private string _applicationName = "LibraryManagement";

    [ObservableProperty]
    private string _welcomeMessage = "Khởi tạo Giai đoạn 1 thành công.";

    public MainViewModel(ILogger<MainViewModel> logger)
    {
        _logger = logger;
        _logger.LogInformation("Đã khởi tạo MainViewModel.");
    }
}
