using CommunityToolkit.Mvvm.Input;

namespace LibraryManagement.App.Navigation;

public interface IRefreshableViewModel
{
    IAsyncRelayCommand RefreshCommand { get; }

    bool IsBusy { get; }
}
