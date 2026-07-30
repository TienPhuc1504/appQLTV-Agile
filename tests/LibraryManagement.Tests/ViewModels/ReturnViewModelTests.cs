using FluentAssertions;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Notifications;
using LibraryManagement.App.ViewModels;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibraryManagement.Tests.ViewModels;

public sealed class ReturnViewModelTests
{
    [Fact]
    public async Task Load_ShouldRetrieveOutstandingBooksWithoutSelectingSlip()
    {
        var returnService = new ReturnServiceStub();
        var viewModel = new ReturnViewModel(
            returnService,
            new BorrowServiceStub(),
            new DialogServiceStub(),
            new NotificationServiceStub(),
            NullLogger<ReturnViewModel>.Instance);

        await viewModel.LoadCommand.ExecuteAsync(null);

        returnService.LastKeyword.Should().BeEmpty();
        viewModel.SearchResults.Should().ContainSingle();
        viewModel.SelectedBorrowSlip.Should().BeNull();
        viewModel.ReturnItems.Should().BeEmpty();
    }

    [Fact]
    public async Task EmptySearch_ShouldRestoreOutstandingBooks()
    {
        var returnService = new ReturnServiceStub();
        var viewModel = new ReturnViewModel(
            returnService,
            new BorrowServiceStub(),
            new DialogServiceStub(),
            new NotificationServiceStub(),
            NullLogger<ReturnViewModel>.Instance);
        returnService.ReturnResults = [];
        viewModel.SearchText = "không tồn tại";
        await viewModel.SearchCommand.ExecuteAsync(null);
        viewModel.SearchResults.Should().BeEmpty();

        returnService.ReturnResults = [CreateLookup()];
        viewModel.SearchText = "   ";
        await viewModel.SearchCommand.ExecuteAsync(null);

        returnService.LastKeyword.Should().BeEmpty();
        viewModel.SearchResults.Should().ContainSingle();
        viewModel.SelectedBorrowSlip.Should().BeNull();
    }

    private static ReturnLookupDto CreateLookup()
    {
        return new ReturnLookupDto(
            1,
            "PM202607-001",
            1,
            "DG0001",
            "Lê Hoàng Nam",
            DateOnly.FromDateTime(DateTime.Today).AddDays(-5),
            [
                new ReturnableBookDto(
                    1,
                    1,
                    "BS001-01",
                    "S0001",
                    "Mắt biếc",
                    DateOnly.FromDateTime(DateTime.Today).AddDays(2),
                    BorrowSlipDetailStatus.Borrowing)
            ]);
    }

    private sealed class ReturnServiceStub : IReturnService
    {
        public string? LastKeyword { get; private set; }

        public IReadOnlyList<ReturnLookupDto> ReturnResults { get; set; } =
            [CreateLookup()];

        public Task<IReadOnlyList<ReturnLookupDto>> SearchOutstandingAsync(
            string keyword,
            CancellationToken cancellationToken = default)
        {
            LastKeyword = keyword;
            return Task.FromResult(ReturnResults);
        }

        public Task<OperationResult> ReturnBookAsync(
            ReturnBookRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> ReturnMultipleBooksAsync(
            ReturnMultipleBooksRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public int CalculateOverdueDays(
            DateOnly expectedReturnDate,
            DateOnly actualReturnDate) =>
            Math.Max(0, actualReturnDate.DayNumber - expectedReturnDate.DayNumber);

        public Task<ReturnPreviewDto> CalculateFineAsync(
            int borrowSlipDetailId,
            PhysicalCondition returnedCondition,
            DateOnly returnDate,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> UpdateBorrowSlipStatusAsync(
            int borrowSlipId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class BorrowServiceStub : IBorrowService
    {
        public Task<BorrowPolicyDto> GetBorrowPolicyAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> ValidateBorrowRequestAsync(
            BorrowCreateRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> ValidateReaderEligibilityAsync(
            int readerId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> CreateBorrowSlipAsync(
            BorrowCreateRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BorrowSlipDto?> GetBorrowSlipAsync(
            int borrowSlipId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PagedResult<BorrowSlipListItemDto>> GetActiveBorrowSlipsAsync(
            BorrowSlipSearchRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<BorrowSlipDetailDto>> GetReaderActiveBorrowsAsync(
            int readerId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> RenewBorrowedBookAsync(
            int borrowSlipDetailId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class DialogServiceStub : IAppDialogService
    {
        public Task ShowMessageAsync(
            string title,
            string message,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> ConfirmAsync(
            string title,
            string message,
            string confirmText = "Xác nhận",
            string cancelText = "Hủy",
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class NotificationServiceStub : IAppNotificationService
    {
        public void Show(
            string title,
            string message,
            NotificationSeverity severity =
                NotificationSeverity.Information)
        {
        }
    }
}
