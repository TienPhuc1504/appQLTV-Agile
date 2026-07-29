using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Notifications;
using LibraryManagement.App.Services;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.ViewModels;

public sealed partial class SettingsViewModel : BaseViewModel
{
    private readonly ISystemSettingService _settingService;
    private readonly IDatabaseBackupService _databaseBackupService;
    private readonly IDatabaseFilePickerService _databaseFilePickerService;
    private readonly IAppDialogService _dialogService;
    private readonly IAppNotificationService _notificationService;
    private readonly ILogger<SettingsViewModel> _logger;

    public SettingsViewModel(
        ISystemSettingService settingService,
        IDatabaseBackupService databaseBackupService,
        IDatabaseFilePickerService databaseFilePickerService,
        IAppDialogService dialogService,
        IAppNotificationService notificationService,
        ILogger<SettingsViewModel> logger)
    {
        _settingService = settingService;
        _databaseBackupService = databaseBackupService;
        _databaseFilePickerService = databaseFilePickerService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public ObservableCollection<SystemSettingDto> Settings { get; } = [];

    [ObservableProperty]
    public partial SystemSettingDto? SelectedSetting { get; set; }

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            LoadSettingsCoreAsync,
            "Đang tải cài đặt hệ thống...",
            cancellationToken);
    }

    [RelayCommand]
    private Task SaveAsync(CancellationToken cancellationToken)
    {
        if (SelectedSetting is null)
        {
            ErrorMessage = "Vui lòng chọn cài đặt cần cập nhật.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                OperationResult result = await _settingService.UpdateAsync(
                    new SystemSettingUpdateRequest(
                        SelectedSetting.Key,
                        Value),
                    token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Lưu cài đặt thành công",
                    "Giá trị mới đã được áp dụng.",
                    NotificationSeverity.Success);
                await LoadSettingsCoreAsync(token);
            },
            "Đang lưu cài đặt...",
            cancellationToken);
    }

    [RelayCommand]
    private Task BackupDatabaseAsync(CancellationToken cancellationToken)
    {
        string? destinationPath =
            _databaseFilePickerService.SelectBackupDestination();
        if (destinationPath is null)
        {
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                DatabaseTransferResult result =
                    await _databaseBackupService.BackupAsync(
                        destinationPath,
                        token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Sao lưu thành công",
                    "Bản sao database đã được tạo tại vị trí đã chọn.",
                    NotificationSeverity.Success);
            },
            "Đang sao lưu cơ sở dữ liệu...",
            cancellationToken);
    }

    [RelayCommand]
    private async Task RestoreDatabaseAsync(CancellationToken cancellationToken)
    {
        string? sourcePath = _databaseFilePickerService.SelectRestoreSource();
        if (sourcePath is null)
        {
            return;
        }

        bool confirmed = await _dialogService.ConfirmAsync(
            "Phục hồi cơ sở dữ liệu",
            "Dữ liệu hiện tại sẽ được thay bằng bản sao đã chọn. Ứng dụng sẽ tạo một bản an toàn trước khi phục hồi. Bạn có muốn tiếp tục?",
            "Phục hồi",
            "Hủy",
            cancellationToken);
        if (!confirmed)
        {
            return;
        }

        await ExecuteBusyAsync(
            async token =>
            {
                DatabaseTransferResult result =
                    await _databaseBackupService.RestoreAsync(sourcePath, token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Phục hồi thành công",
                    "Hãy đăng xuất hoặc khởi động lại ứng dụng để nạp toàn bộ dữ liệu.",
                    NotificationSeverity.Success);
            },
            "Đang phục hồi cơ sở dữ liệu...",
            cancellationToken);
    }

    partial void OnSelectedSettingChanged(SystemSettingDto? value)
    {
        Value = value?.Value ?? string.Empty;
        ErrorMessage = null;
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        _logger.LogError(exception, "Không thể xử lý cài đặt hệ thống.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể xử lý cài đặt hệ thống. Vui lòng thử lại.";
    }

    private async Task LoadSettingsCoreAsync(
        CancellationToken cancellationToken)
    {
        string? selectedKey = SelectedSetting?.Key;
        IReadOnlyList<SystemSettingDto> settings =
            await _settingService.GetAllAsync(cancellationToken);
        Settings.Clear();
        foreach (SystemSettingDto setting in settings)
        {
            Settings.Add(setting);
        }

        SelectedSetting = Settings.FirstOrDefault(
                setting => setting.Key == selectedKey)
            ?? Settings.FirstOrDefault();
    }
}
