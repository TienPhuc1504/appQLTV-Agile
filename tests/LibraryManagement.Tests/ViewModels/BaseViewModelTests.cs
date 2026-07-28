using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAssertions;
using LibraryManagement.App.ViewModels;

namespace LibraryManagement.Tests.ViewModels;

public sealed partial class BaseViewModelTests
{
    [Fact]
    public async Task ExecuteBusyAsync_ShouldResetBusyStateAfterSuccess()
    {
        var viewModel = new TestViewModel();

        Task operation = viewModel.RunAsync(() => Task.CompletedTask);

        await operation;

        viewModel.IsBusy.Should().BeFalse();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteBusyAsync_ShouldExposeFriendlyErrorAndResetBusyState()
    {
        var viewModel = new TestViewModel();

        await viewModel.RunAsync(
            () => Task.FromException(new InvalidOperationException("Chi tiết kỹ thuật")));

        viewModel.IsBusy.Should().BeFalse();
        viewModel.ErrorMessage.Should().Be("Đã xảy ra lỗi. Vui lòng thử lại.");
    }

    [Fact]
    public void Validate_ShouldReturnFalseForInvalidAnnotatedProperty()
    {
        var viewModel = new TestViewModel
        {
            RequiredText = string.Empty
        };

        bool result = viewModel.Validate();

        result.Should().BeFalse();
        viewModel.GetErrors(nameof(TestViewModel.RequiredText))
            .Cast<ValidationResult>()
            .Should()
            .ContainSingle(resultItem =>
                resultItem.ErrorMessage == "Vui lòng nhập dữ liệu.");
    }

    public sealed partial class TestViewModel : BaseViewModel
    {
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Vui lòng nhập dữ liệu.")]
        public partial string RequiredText { get; set; } = "Hợp lệ";

        public Task RunAsync(Func<Task> operation)
        {
            return ExecuteBusyAsync(_ => operation());
        }
    }
}
