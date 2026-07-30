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

public sealed partial class PublisherViewModel(
    IPublisherService publisherService,
    IAppDialogService dialogService,
    IAppNotificationService notificationService,
    ILogger<PublisherViewModel> logger)
    : CatalogViewModelBase<PublisherDto>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorTitle))]
    [NotifyPropertyChangedFor(nameof(EditorDescription))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusActionText))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleStatusCommand))]
    public partial PublisherDto? SelectedItem { get; set; }

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
    [Required(ErrorMessage = "Vui lòng nhập tên nhà xuất bản.")]
    [MaxLength(200, ErrorMessage = "Tên nhà xuất bản không được vượt quá 200 ký tự.")]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
    public partial string? Address { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [RegularExpression(
        @"^\+?[0-9][0-9 .-]{7,19}$",
        ErrorMessage = "Số điện thoại không đúng định dạng.")]
    public partial string? PhoneNumber { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [MaxLength(254, ErrorMessage = "Email không được vượt quá 254 ký tự.")]
    public partial string? Email { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [RegularExpression(
        @"^https?://.+$",
        ErrorMessage = "Website phải là địa chỉ HTTP hoặc HTTPS hợp lệ.")]
    [MaxLength(300, ErrorMessage = "Website không được vượt quá 300 ký tự.")]
    public partial string? Website { get; set; }

    public bool IsEmptyMode => EditorMode == CatalogEditorMode.Empty;

    public bool IsCreateMode => EditorMode == CatalogEditorMode.Create;

    public bool IsEditMode => EditorMode == CatalogEditorMode.Edit;

    public bool IsFormMode => IsCreateMode || IsEditMode;

    public string EditorEyebrow =>
        IsCreateMode ? "THÊM MỚI" : "CHỈNH SỬA";

    public string EditorTitle =>
        IsCreateMode ? "Thêm nhà xuất bản" : "Chỉnh sửa nhà xuất bản";

    public string EditorDescription
    {
        get
        {
            if (IsCreateMode)
            {
                return "Nhập thông tin để tạo nhà xuất bản mới.";
            }

            return string.IsNullOrWhiteSpace(Name)
                ? "Cập nhật thông tin nhà xuất bản đã chọn."
                : $"Cập nhật thông tin của nhà xuất bản “{Name}”.";
        }
    }

    public string SaveActionText =>
        IsCreateMode ? "Tạo nhà xuất bản" : "Lưu thay đổi";

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
            ErrorMessage = "Vui lòng kiểm tra lại thông tin nhà xuất bản.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                bool isCreating = IsCreateMode;
                int? selectedItemId = SelectedItem?.Id;
                var request = new PublisherUpsertRequest(
                    Name,
                    Address,
                    PhoneNumber,
                    Email,
                    Website,
                    SelectedItem?.IsActive ?? true);
                OperationResult result = SelectedItem is null
                    ? await publisherService.CreateAsync(request, token)
                    : await publisherService.UpdateAsync(
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
                        ? "Nhà xuất bản mới đã được tạo."
                        : "Thông tin nhà xuất bản đã được cập nhật.",
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
            "Đang lưu nhà xuất bản...",
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
        PublisherDto selectedItem = SelectedItem!;
        string action = selectedItem.IsActive ? "ngừng sử dụng" : "kích hoạt";

        return ExecuteBusyAsync(
            async token =>
            {
                bool confirmed = await dialogService.ConfirmAsync(
                    "Xác nhận thay đổi trạng thái",
                    $"Bạn có chắc muốn {action} nhà xuất bản “{selectedItem.Name}”?",
                    "Xác nhận",
                    "Hủy",
                    token);
                if (!confirmed)
                {
                    return;
                }

                OperationResult result = await publisherService.SetActiveAsync(
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
                    $"Đã {action} nhà xuất bản.",
                    NotificationSeverity.Success);
                await RefreshItemsAsync(token);
                ResetToEmpty();
            },
            "Đang cập nhật trạng thái...",
            cancellationToken);
    }

    protected override Task<IReadOnlyList<PublisherDto>> SearchCoreAsync(
        string? keyword,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        return publisherService.SearchAsync(
            keyword,
            includeInactive,
            cancellationToken);
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        logger.LogError(exception, "Không thể xử lý danh mục nhà xuất bản.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể xử lý danh mục nhà xuất bản. Vui lòng thử lại.";
    }

    partial void OnSelectedItemChanged(PublisherDto? value)
    {
        if (value is null)
        {
            ClearEditor();
            EditorMode = CatalogEditorMode.Empty;
            return;
        }

        Name = value.Name;
        Address = value.Address;
        PhoneNumber = value.PhoneNumber;
        Email = value.Email;
        Website = value.Website;
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
        Name = string.Empty;
        Address = null;
        PhoneNumber = null;
        Email = null;
        Website = null;
        ClearValidation();
        ErrorMessage = null;
    }
}
