using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LibraryManagement.App.ViewModels;

public abstract partial class CatalogViewModelBase<TItem> : BaseViewModel, IDisposable
{
    private CancellationTokenSource? _searchDelayCancellation;
    private bool _disposed;

    public ObservableCollection<TItem> Items { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IncludeInactive { get; set; }

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            token => RefreshItemsAsync(token),
            "Đang tải dữ liệu...",
            cancellationToken);
    }

    [RelayCommand]
    private Task SearchAsync(CancellationToken cancellationToken)
    {
        CancelPendingSearch();
        return ExecuteBusyAsync(
            token => RefreshItemsAsync(token),
            "Đang tìm kiếm...",
            cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CancelPendingSearch();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    protected async Task RefreshItemsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TItem> items = await SearchCoreAsync(
            SearchText,
            IncludeInactive,
            cancellationToken);
        Items.Clear();
        foreach (TItem item in items)
        {
            Items.Add(item);
        }
    }

    protected abstract Task<IReadOnlyList<TItem>> SearchCoreAsync(
        string? keyword,
        bool includeInactive,
        CancellationToken cancellationToken);

    partial void OnSearchTextChanged(string value)
    {
        ScheduleSearch(TimeSpan.FromMilliseconds(400));
    }

    partial void OnIncludeInactiveChanged(bool value)
    {
        ScheduleSearch(TimeSpan.Zero);
    }

    private void ScheduleSearch(TimeSpan delay)
    {
        if (_disposed)
        {
            return;
        }

        CancelPendingSearch();
        _searchDelayCancellation = new CancellationTokenSource();
        _ = RunScheduledSearchAsync(delay, _searchDelayCancellation.Token);
    }

    private async Task RunScheduledSearchAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await ExecuteBusyAsync(
                RefreshItemsAsync,
                "Đang tìm kiếm...",
                CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CancelPendingSearch()
    {
        _searchDelayCancellation?.Cancel();
        _searchDelayCancellation?.Dispose();
        _searchDelayCancellation = null;
    }
}
