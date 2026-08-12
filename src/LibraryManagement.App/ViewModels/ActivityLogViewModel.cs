using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryManagement.App.Navigation;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.ViewModels;

public sealed partial class ActivityLogViewModel :
    BaseViewModel,
    IRefreshableViewModel
{
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<ActivityLogViewModel> _logger;

    public ActivityLogViewModel(
        IActivityLogService activityLogService,
        ILogger<ActivityLogViewModel> logger)
    {
        _activityLogService = activityLogService;
        _logger = logger;
    }

    IAsyncRelayCommand IRefreshableViewModel.RefreshCommand => LoadCommand;

    public ObservableCollection<ActivityLogDto> ActivityLogs { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ActionFilter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int? EmployeeIdFilter { get; set; }

    [ObservableProperty]
    public partial DateTime? FromDate { get; set; }

    [ObservableProperty]
    public partial DateTime? ToDate { get; set; }

    [ObservableProperty]
    public partial int PageNumber { get; private set; } = 1;

    [ObservableProperty]
    public partial int TotalPages { get; private set; } = 1;

    [ObservableProperty]
    public partial int TotalCount { get; private set; }

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        PageNumber = 1;
        return ExecuteBusyAsync(
            LoadPageCoreAsync,
            "Đang tải nhật ký hoạt động...",
            cancellationToken);
    }

    [RelayCommand]
    private Task SearchAsync(CancellationToken cancellationToken)
    {
        PageNumber = 1;
        return ExecuteBusyAsync(
            LoadPageCoreAsync,
            "Đang tìm nhật ký...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task PreviousPageAsync(CancellationToken cancellationToken)
    {
        PageNumber--;
        return ExecuteBusyAsync(
            LoadPageCoreAsync,
            "Đang tải trang trước...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextPageAsync(CancellationToken cancellationToken)
    {
        PageNumber++;
        return ExecuteBusyAsync(
            LoadPageCoreAsync,
            "Đang tải trang sau...",
            cancellationToken);
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        _logger.LogError(exception, "Không thể tải nhật ký hoạt động.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể tải nhật ký hoạt động. Vui lòng thử lại.";
    }

    private async Task LoadPageCoreAsync(CancellationToken cancellationToken)
    {
        DateTime? from = FromDate?.Date;
        DateTime? to = ToDate?.Date.AddDays(1).AddTicks(-1);
        PagedResult<ActivityLogDto> result =
            await _activityLogService.SearchAsync(
                new ActivityLogSearchRequest(
                    Keyword: SearchText,
                    EmployeeId: EmployeeIdFilter,
                    Action: ActionFilter,
                    From: from,
                    To: to,
                    PageNumber: PageNumber,
                    PageSize: 20),
                cancellationToken);
        ActivityLogs.Clear();
        foreach (ActivityLogDto log in result.Items)
        {
            ActivityLogs.Add(log);
        }

        PageNumber = result.PageNumber;
        TotalPages = result.TotalPages;
        TotalCount = result.TotalCount;
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    private bool CanGoPrevious() => PageNumber > 1;

    private bool CanGoNext() => PageNumber < TotalPages;
}
