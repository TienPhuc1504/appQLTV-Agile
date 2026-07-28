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

public sealed partial class AuthorViewModel(
    IAuthorService authorService,
    IAppDialogService dialogService,
    IAppNotificationService notificationService,
    ILogger<AuthorViewModel> logger)
    : CatalogViewModelBase<AuthorDto>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorTitle))]
    [NotifyPropertyChangedFor(nameof(StatusActionText))]
    [NotifyCanExecuteChangedFor(nameof(ToggleStatusCommand))]
    public partial AuthorDto? SelectedItem { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập họ tên tác giả.")]
    [MaxLength(150, ErrorMessage = "Họ tên tác giả không được vượt quá 150 ký tự.")]
    public partial string FullName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(
        typeof(AuthorViewModel),
        nameof(ValidateDateOfBirth))]
    public partial DateTime? DateOfBirth { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(100, ErrorMessage = "Quốc tịch không được vượt quá 100 ký tự.")]
    public partial string? Nationality { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(4000, ErrorMessage = "Tiểu sử không được vượt quá 4000 ký tự.")]
    public partial string? Biography { get; set; }

    public string EditorTitle =>
        SelectedItem is null ? "Thêm tác giả" : "Cập nhật tác giả";

    public string StatusActionText =>
        SelectedItem?.IsActive == true ? "Ngừng sử dụng" : "Kích hoạt";

    public static ValidationResult? ValidateDateOfBirth(
        DateTime? value,
        ValidationContext context)
    {
        return value?.Date > DateTime.Today
            ? new ValidationResult("Ngày sinh không được lớn hơn ngày hiện tại.")
            : ValidationResult.Success;
    }

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
            ErrorMessage = "Vui lòng kiểm tra lại thông tin tác giả.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                var request = new AuthorUpsertRequest(
                    FullName,
                    DateOfBirth.HasValue
                        ? DateOnly.FromDateTime(DateOfBirth.Value)
                        : null,
                    Nationality,
                    Biography,
                    SelectedItem?.IsActive ?? true);
                OperationResult result = SelectedItem is null
                    ? await authorService.CreateAsync(request, token)
                    : await authorService.UpdateAsync(
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
                    "Thông tin tác giả đã được cập nhật.",
                    NotificationSeverity.Success);
                SelectedItem = null;
                ClearEditor();
                await RefreshItemsAsync(token);
            },
            "Đang lưu tác giả...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanToggleStatus))]
    private Task ToggleStatusAsync(CancellationToken cancellationToken)
    {
        AuthorDto selectedItem = SelectedItem!;
        string action = selectedItem.IsActive ? "ngừng sử dụng" : "kích hoạt";

        return ExecuteBusyAsync(
            async token =>
            {
                bool confirmed = await dialogService.ConfirmAsync(
                    "Xác nhận thay đổi trạng thái",
                    $"Bạn có chắc muốn {action} tác giả “{selectedItem.FullName}”?",
                    "Xác nhận",
                    "Hủy",
                    token);
                if (!confirmed)
                {
                    return;
                }

                OperationResult result = await authorService.SetActiveAsync(
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
                    $"Đã {action} tác giả.",
                    NotificationSeverity.Success);
                SelectedItem = null;
                ClearEditor();
                await RefreshItemsAsync(token);
            },
            "Đang cập nhật trạng thái...",
            cancellationToken);
    }

    protected override Task<IReadOnlyList<AuthorDto>> SearchCoreAsync(
        string? keyword,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        return authorService.SearchAsync(
            keyword,
            includeInactive,
            cancellationToken);
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        logger.LogError(exception, "Không thể xử lý danh mục tác giả.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể xử lý danh mục tác giả. Vui lòng thử lại.";
    }

    partial void OnSelectedItemChanged(AuthorDto? value)
    {
        if (value is null)
        {
            ClearEditor();
            return;
        }

        FullName = value.FullName;
        DateOfBirth = value.DateOfBirth.HasValue
            ? value.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue)
            : null;
        Nationality = value.Nationality;
        Biography = value.Biography;
        ClearValidation();
        ErrorMessage = null;
    }

    private bool CanToggleStatus() => SelectedItem is not null;

    private void ClearEditor()
    {
        FullName = string.Empty;
        DateOfBirth = null;
        Nationality = null;
        Biography = null;
        ClearValidation();
        ErrorMessage = null;
    }
}
