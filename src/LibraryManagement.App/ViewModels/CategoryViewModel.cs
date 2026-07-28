using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Notifications;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.ViewModels;

public sealed partial class CategoryViewModel(
    ICategoryService categoryService,
    IAppDialogService dialogService,
    IAppNotificationService notificationService,
    ILogger<CategoryViewModel> logger)
    : CatalogViewModelBase<CategoryDto>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorTitle))]
    [NotifyPropertyChangedFor(nameof(StatusActionText))]
    [NotifyCanExecuteChangedFor(nameof(ToggleStatusCommand))]
    public partial CategoryDto? SelectedItem { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập tên thể loại.")]
    [MaxLength(100, ErrorMessage = "Tên thể loại không được vượt quá 100 ký tự.")]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    public partial string? Description { get; set; }

    public string EditorTitle =>
        SelectedItem is null ? "Thêm thể loại" : "Cập nhật thể loại";

    public string StatusActionText =>
        SelectedItem?.IsActive == true ? "Ngừng sử dụng" : "Kích hoạt";

    [RelayCommand]
    private void New()
    {
        SelectedItem = null;
        ClearEditor();
    }

    [RelayCommand]
    private Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!Validate())
        {
            ErrorMessage = "Vui lòng kiểm tra lại thông tin thể loại.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                var request = new CategoryUpsertRequest(
                    Name,
                    Description,
                    SelectedItem?.IsActive ?? true);
                OperationResult result = SelectedItem is null
                    ? await categoryService.CreateAsync(request, token)
                    : await categoryService.UpdateAsync(
                        SelectedItem.Id,
                        request,
                        token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                notificationService.Show(
                    "Lưu thành công",
                    "Thông tin thể loại đã được cập nhật.",
                    NotificationSeverity.Success);
                SelectedItem = null;
                ClearEditor();
                await RefreshItemsAsync(token);
            },
            "Đang lưu thể loại...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanToggleStatus))]
    private Task ToggleStatusAsync(CancellationToken cancellationToken)
    {
        CategoryDto selectedItem = SelectedItem!;
        string action = selectedItem.IsActive ? "ngừng sử dụng" : "kích hoạt";

        return ExecuteBusyAsync(
            async token =>
            {
                bool confirmed = await dialogService.ConfirmAsync(
                    "Xác nhận thay đổi trạng thái",
                    $"Bạn có chắc muốn {action} thể loại “{selectedItem.Name}”?",
                    "Xác nhận",
                    "Hủy",
                    token);
                if (!confirmed)
                {
                    return;
                }

                OperationResult result = await categoryService.SetActiveAsync(
                    selectedItem.Id,
                    !selectedItem.IsActive,
                    token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                notificationService.Show(
                    "Cập nhật thành công",
                    $"Đã {action} thể loại.",
                    NotificationSeverity.Success);
                SelectedItem = null;
                ClearEditor();
                await RefreshItemsAsync(token);
            },
            "Đang cập nhật trạng thái...",
            cancellationToken);
    }

    protected override Task<IReadOnlyList<CategoryDto>> SearchCoreAsync(
        string? keyword,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        return categoryService.SearchAsync(
            keyword,
            includeInactive,
            cancellationToken);
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        logger.LogError(exception, "Không thể xử lý danh mục thể loại.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể xử lý danh mục thể loại. Vui lòng thử lại.";
    }

    partial void OnSelectedItemChanged(CategoryDto? value)
    {
        if (value is null)
        {
            ClearEditor();
            return;
        }

        Name = value.Name;
        Description = value.Description;
        ClearValidation();
        ErrorMessage = null;
    }

    private bool CanToggleStatus() => SelectedItem is not null;

    private void ClearEditor()
    {
        Name = string.Empty;
        Description = null;
        ClearValidation();
        ErrorMessage = null;
    }
}
