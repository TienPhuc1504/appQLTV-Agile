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
    [NotifyPropertyChangedFor(nameof(EditorDescription))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusActionText))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleStatusCommand))]
    public partial CategoryDto? SelectedItem { get; set; }

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
    public partial CategoryEditorMode EditorMode { get; private set; } =
        CategoryEditorMode.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(EditorDescription))]
    [Required(ErrorMessage = "Vui lòng nhập tên thể loại.")]
    [MaxLength(100, ErrorMessage = "Tên thể loại không được vượt quá 100 ký tự.")]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    public partial string? Description { get; set; }

    public bool IsEmptyMode => EditorMode == CategoryEditorMode.Empty;

    public bool IsCreateMode => EditorMode == CategoryEditorMode.Create;

    public bool IsEditMode => EditorMode == CategoryEditorMode.Edit;

    public bool IsFormMode => IsCreateMode || IsEditMode;

    public string EditorEyebrow =>
        IsCreateMode ? "THÊM MỚI" : "CHỈNH SỬA";

    public string EditorTitle =>
        IsCreateMode ? "Thêm thể loại" : "Chỉnh sửa thể loại";

    public string EditorDescription
    {
        get
        {
            if (IsCreateMode)
            {
                return "Nhập thông tin để tạo thể loại mới.";
            }

            return string.IsNullOrWhiteSpace(Name)
                ? "Cập nhật thông tin thể loại đã chọn."
                : $"Cập nhật thông tin của thể loại “{Name}”.";
        }
    }

    public string SaveActionText =>
        IsCreateMode ? "Tạo thể loại" : "Lưu thay đổi";

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
        EditorMode = CategoryEditorMode.Create;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
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
                bool isCreating = IsCreateMode;
                int? selectedItemId = SelectedItem?.Id;
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
                    isCreating ? "Tạo thành công" : "Lưu thành công",
                    isCreating
                        ? "Thể loại mới đã được tạo."
                        : "Thông tin thể loại đã được cập nhật.",
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
            "Đang lưu thể loại...",
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
                await RefreshItemsAsync(token);
                ResetToEmpty();
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
            EditorMode = CategoryEditorMode.Empty;
            return;
        }

        Name = value.Name;
        Description = value.Description;
        ClearValidation();
        ErrorMessage = null;
        EditorMode = CategoryEditorMode.Edit;
    }

    private bool CanSave() =>
        IsCreateMode || (IsEditMode && SelectedItem is not null);

    private bool CanToggleStatus() =>
        IsEditMode && SelectedItem is not null;

    private void ResetToEmpty()
    {
        SelectedItem = null;
        ClearEditor();
        EditorMode = CategoryEditorMode.Empty;
    }

    private void ClearEditor()
    {
        Name = string.Empty;
        Description = null;
        ClearValidation();
        ErrorMessage = null;
    }
}
