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
    [NotifyPropertyChangedFor(nameof(EditorDescription))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusActionText))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleStatusCommand))]
    public partial AuthorDto? SelectedItem { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyMode))]
    [NotifyPropertyChangedFor(nameof(IsCreateMode))]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(IsFormMode))]
    [NotifyPropertyChangedFor(nameof(EditorEyebrow))]
    [NotifyPropertyChangedFor(nameof(EditorTitle))]
    [NotifyPropertyChangedFor(nameof(EditorDescription))]
    [NotifyPropertyChangedFor(nameof(SaveActionText))]
    [NotifyPropertyChangedFor(nameof(CancelActionText))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleStatusCommand))]
    public partial CatalogEditorMode EditorMode { get; private set; } =
        CatalogEditorMode.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(EditorDescription))]
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

    public bool IsEmptyMode => EditorMode == CatalogEditorMode.Empty;

    public bool IsCreateMode => EditorMode == CatalogEditorMode.Create;

    public bool IsEditMode => EditorMode == CatalogEditorMode.Edit;

    public bool IsFormMode => IsCreateMode || IsEditMode;

    public string EditorEyebrow =>
        IsCreateMode ? "THÊM MỚI" : "CHỈNH SỬA";

    public string EditorTitle =>
        IsCreateMode ? "Thêm tác giả" : "Chỉnh sửa tác giả";

    public string EditorDescription
    {
        get
        {
            if (IsCreateMode)
            {
                return "Nhập thông tin để tạo tác giả mới.";
            }

            return string.IsNullOrWhiteSpace(FullName)
                ? "Cập nhật thông tin tác giả đã chọn."
                : $"Cập nhật thông tin của tác giả “{FullName}”.";
        }
    }

    public string SaveActionText =>
        IsCreateMode ? "Tạo tác giả" : "Lưu thay đổi";

    public string CancelActionText =>
        IsCreateMode ? "Hủy" : "Hủy thay đổi";

    public string StatusText =>
        SelectedItem?.IsActive == true
            ? "Đang hoạt động"
            : "Ngừng sử dụng";

    public string StatusActionText =>
        SelectedItem?.IsActive == true
            ? "Ngừng sử dụng"
            : "Kích hoạt lại";

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
        EditorMode = CatalogEditorMode.Create;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
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
                bool isCreating = IsCreateMode;
                int? selectedItemId = SelectedItem?.Id;
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
                    isCreating ? "Tạo thành công" : "Lưu thành công",
                    isCreating
                        ? "Tác giả mới đã được tạo."
                        : "Thông tin tác giả đã được cập nhật.",
                    NotificationSeverity.Success);
                await RefreshItemsAsync(token);

                if (!isCreating && selectedItemId.HasValue)
                {
                    SelectedItem = Items.FirstOrDefault(
                        item => item.Id == selectedItemId.Value);
                    if (SelectedItem is not null)
                    {
                        return;
                    }
                }

                ResetToEmpty();
            },
            "Đang lưu tác giả...",
            cancellationToken);
    }

    [RelayCommand]
    private void Cancel()
    {
        ResetToEmpty();
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
                await RefreshItemsAsync(token);
                ResetToEmpty();
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
            EditorMode = CatalogEditorMode.Empty;
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
        EditorMode = CatalogEditorMode.Edit;
    }

    private bool CanSave() =>
        IsCreateMode || (IsEditMode && SelectedItem is not null);

    private bool CanToggleStatus() =>
        IsEditMode && SelectedItem is not null;

    private void ResetToEmpty()
    {
        SelectedItem = null;
        ClearEditor();
        EditorMode = CatalogEditorMode.Empty;
    }

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
