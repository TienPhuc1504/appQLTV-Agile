using CommunityToolkit.Mvvm.ComponentModel;

namespace LibraryManagement.App.ViewModels;

public abstract partial class BaseViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; protected set; }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial string BusyMessage { get; private set; } = "Đang xử lý...";

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    public void ClearValidation()
    {
        ClearErrors();
    }

    protected async Task ExecuteBusyAsync(
        Func<CancellationToken, Task> operation,
        string busyMessage = "Đang xử lý...",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        BusyMessage = string.IsNullOrWhiteSpace(busyMessage)
            ? "Đang xử lý..."
            : busyMessage;
        IsBusy = true;

        try
        {
            await operation(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ErrorMessage = "Thao tác đã bị hủy.";
        }
        catch (Exception exception)
        {
            ErrorMessage = GetFriendlyErrorMessage(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected virtual string GetFriendlyErrorMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return "Đã xảy ra lỗi. Vui lòng thử lại.";
    }
}
