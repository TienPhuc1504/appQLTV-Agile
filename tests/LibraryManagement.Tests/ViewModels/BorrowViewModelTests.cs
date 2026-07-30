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

public sealed class BorrowViewModelTests
{
    [Fact]
    public async Task Load_ShouldPopulateReadersAndOnlyAvailableCopiesWithoutSelectingReader()
    {
        var readerService = new ReaderServiceStub();
        var bookCopyService = new BookCopyServiceStub();
        BorrowViewModel viewModel = CreateViewModel(
            readerService,
            bookCopyService);

        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.ReaderResults.Should().HaveCount(2);
        viewModel.SelectedReader.Should().BeNull();
        viewModel.AvailableCopies.Should().HaveCount(2);
        viewModel.AvailableCopies.Should().OnlyContain(
            copy => copy.Status == BookCopyStatus.Available);
        bookCopyService.LastRequest.Should().NotBeNull();
        bookCopyService.LastRequest!.Status.Should().Be(BookCopyStatus.Available);
    }

    [Fact]
    public async Task EmptySearches_ShouldRestoreDefaultReaderAndCopyLists()
    {
        BorrowViewModel viewModel = CreateViewModel(
            new ReaderServiceStub(),
            new BookCopyServiceStub());
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.ReaderSearchText = "DG0004";
        await viewModel.SearchReaderCommand.ExecuteAsync(null);
        viewModel.ReaderResults.Should().ContainSingle();

        viewModel.ReaderSearchText = "   ";
        await viewModel.SearchReaderCommand.ExecuteAsync(null);
        viewModel.ReaderResults.Should().HaveCount(2);
        viewModel.ReaderEmptyMessage.Should().Be("Chưa có độc giả.");

        viewModel.CopyCode = "Mắt biếc";
        await viewModel.SearchBookCopiesCommand.ExecuteAsync(null);
        viewModel.AvailableCopies.Should().ContainSingle();

        viewModel.CopyCode = " ";
        await viewModel.SearchBookCopiesCommand.ExecuteAsync(null);
        viewModel.AvailableCopies.Should().HaveCount(2);
        viewModel.AvailableCopyEmptyMessage.Should()
            .Be("Chưa có bản sách khả dụng.");
    }

    [Fact]
    public async Task Searches_ShouldTrimAndFilterReaderAndAvailableCopy()
    {
        var readerService = new ReaderServiceStub();
        var bookCopyService = new BookCopyServiceStub();
        BorrowViewModel viewModel = CreateViewModel(
            readerService,
            bookCopyService);
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.ReaderSearchText = "  Nguyễn Bảo  ";
        await viewModel.SearchReaderCommand.ExecuteAsync(null);

        viewModel.ReaderResults.Should().ContainSingle();
        viewModel.ReaderResults[0].ReaderCode.Should().Be("DG0004");
        readerService.LastKeyword.Should().Be("Nguyễn Bảo");

        viewModel.CopyCode = "  Clean  ";
        await viewModel.SearchBookCopiesCommand.ExecuteAsync(null);

        viewModel.AvailableCopies.Should().ContainSingle();
        viewModel.AvailableCopies[0].CopyCode.Should().Be("BS002-01");
        bookCopyService.LastRequest!.Keyword.Should().Be("Clean");
        bookCopyService.LastRequest.Status.Should().Be(BookCopyStatus.Available);
    }

    [Fact]
    public async Task SelectingReader_ShouldUpdateSelectionAndRunEligibility()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        var borrowService = new BorrowServiceStub();
        BorrowViewModel viewModel = CreateViewModel(
            new ReaderServiceStub(),
            new BookCopyServiceStub(),
            borrowService);
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedReader = viewModel.ReaderResults[1];
        await WaitUntilAsync(
            () => viewModel.IsReaderEligible,
            cancellationToken);

        viewModel.SelectedReader.ReaderCode.Should().Be("DG0005");
        borrowService.LastEligibilityReaderId.Should()
            .Be(viewModel.SelectedReader.Id);
    }

    [Fact]
    public async Task AddSelectedCopy_ShouldRemoveItFromAvailableListAndRejectDuplicate()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        BorrowViewModel viewModel = CreateViewModel(
            new ReaderServiceStub(),
            new BookCopyServiceStub());
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedReader = viewModel.ReaderResults[0];
        await WaitUntilAsync(
            () => viewModel.IsReaderEligible,
            cancellationToken);
        BookCopyDto copy = viewModel.AvailableCopies[0];

        viewModel.SelectedAvailableCopy = copy;
        await viewModel.AddCopyCommand.ExecuteAsync(null);

        viewModel.SelectedCopies.Should().ContainSingle().Which.Should().Be(copy);
        viewModel.AvailableCopies.Should().NotContain(
            item => item.Id == copy.Id);
        viewModel.SelectedAvailableCopy.Should().BeNull();

        viewModel.SelectedAvailableCopy = copy;
        await viewModel.AddCopyCommand.ExecuteAsync(null);

        viewModel.SelectedCopies.Should().ContainSingle();
        viewModel.ErrorMessage.Should()
            .Be("Bản sách đã có trong danh sách mượn.");
    }

    [Fact]
    public async Task RemoveSelectedCopy_ShouldRestoreItToAvailableList()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        BorrowViewModel viewModel = CreateViewModel(
            new ReaderServiceStub(),
            new BookCopyServiceStub());
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedReader = viewModel.ReaderResults[0];
        await WaitUntilAsync(
            () => viewModel.IsReaderEligible,
            cancellationToken);
        BookCopyDto copy = viewModel.AvailableCopies[0];
        viewModel.SelectedAvailableCopy = copy;
        await viewModel.AddCopyCommand.ExecuteAsync(null);

        viewModel.SelectedCartCopy = copy;
        viewModel.RemoveCopyCommand.Execute(null);

        viewModel.SelectedCopies.Should().BeEmpty();
        viewModel.AvailableCopies.Should().Contain(
            item => item.Id == copy.Id);
        viewModel.ConfirmBorrowCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task BorrowFlow_AfterSelectingReaderAndAddingCopy_ShouldEnableConfirm()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        var readerService = new ReaderServiceStub();
        var bookCopyService = new BookCopyServiceStub();
        var borrowService = new BorrowServiceStub();
        var viewModel = new BorrowViewModel(
            readerService,
            bookCopyService,
            borrowService,
            new DialogServiceStub(),
            new NotificationServiceStub(),
            NullLogger<BorrowViewModel>.Instance);

        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.ReaderSearchText = "  DG0004  ";
        await viewModel.SearchReaderCommand.ExecuteAsync(null);
        await WaitUntilAsync(
            () => viewModel.IsReaderEligible,
            cancellationToken);

        viewModel.SelectedReader.Should().NotBeNull();
        viewModel.SelectedReader!.ReaderCode.Should().Be("DG0004");
        readerService.LastKeyword.Should().Be("DG0004");
        borrowService.LastEligibilityReaderId.Should().Be(4);
        viewModel.EligibilityMessage.Should().Contain("có thể mượn thêm 5");

        viewModel.CopyCode = "  BS001-02  ";
        viewModel.AddCopyCommand.CanExecute(null).Should().BeTrue();
        await viewModel.AddCopyCommand.ExecuteAsync(null);

        viewModel.SelectedCopies.Should().ContainSingle();
        viewModel.SelectedCopySummary.Should().Be("1/5 bản sách trong phiếu");
        viewModel.ConfirmBorrowCommand.CanExecute(null).Should().BeTrue();

        viewModel.SelectedCartCopy = viewModel.SelectedCopies.Single();
        viewModel.RemoveCopyCommand.CanExecute(null).Should().BeTrue();
        viewModel.RemoveCopyCommand.Execute(null);

        viewModel.SelectedCopies.Should().BeEmpty();
        viewModel.ConfirmBorrowCommand.CanExecute(null).Should().BeFalse();
    }

    private static BorrowViewModel CreateViewModel(
        ReaderServiceStub readerService,
        BookCopyServiceStub bookCopyService,
        BorrowServiceStub? borrowService = null)
    {
        return new BorrowViewModel(
            readerService,
            bookCopyService,
            borrowService ?? new BorrowServiceStub(),
            new DialogServiceStub(),
            new NotificationServiceStub(),
            NullLogger<BorrowViewModel>.Instance);
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20, cancellationToken);
        }

        condition().Should().BeTrue(
            "trạng thái eligibility phải được cập nhật sau khi chọn độc giả");
    }

    private sealed class ReaderServiceStub : IReaderService
    {
        private static readonly IReadOnlyList<ReaderListItemDto> Readers =
        [
            new(
                4,
                "DG0004",
                "Nguyễn Bảo Ngọc",
                new DateOnly(2000, 1, 1),
                Gender.Female,
                "0901234567",
                "reader@example.com",
                ReaderType.Student,
                DateOnly.FromDateTime(DateTime.Today).AddMonths(6),
                ReaderStatus.Active),
            new(
                5,
                "DG0005",
                "Trần Minh An",
                new DateOnly(1995, 2, 2),
                Gender.Male,
                "0912345678",
                "reader2@example.com",
                ReaderType.Adult,
                DateOnly.FromDateTime(DateTime.Today).AddMonths(4),
                ReaderStatus.Active)
        ];

        public string? LastKeyword { get; private set; }

        public Task<PagedResult<ReaderListItemDto>> SearchAsync(
            ReaderSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            LastKeyword = request.Keyword;
            string? keyword = request.Keyword?.Trim();
            ReaderListItemDto[] readers = Readers
                .Where(reader =>
                    string.IsNullOrWhiteSpace(keyword)
                    || reader.ReaderCode.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase)
                    || reader.FullName.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase)
                    || (reader.PhoneNumber?.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) ?? false)
                    || (reader.Email?.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) ?? false))
                .ToArray();
            return Task.FromResult(
                new PagedResult<ReaderListItemDto>(
                    readers,
                    readers.Length,
                    1,
                    request.PageSize));
        }

        public Task<PagedResult<ReaderListItemDto>> GetAllAsync(
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ReaderDetailDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> CreateAsync(
            ReaderUpsertRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> UpdateAsync(
            int id,
            ReaderUpsertRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> LockAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> UnlockAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> RenewCardAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DateOnly> GetSuggestedExpirationDateAsync(
            DateOnly registeredAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ReaderBorrowHistoryDto>>
            GetBorrowingHistoryAsync(
                int readerId,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ReaderFineDto>> GetOutstandingFinesAsync(
            int readerId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> ValidateBorrowEligibilityAsync(
            int readerId,
            DateOnly? evaluationDate = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class BookCopyServiceStub : IBookCopyService
    {
        private static readonly IReadOnlyList<BookCopyDto> Copies =
        [
            CreateCopy(
                2,
                "BS001-02",
                "S0001",
                "Mắt biếc",
                "Kệ 1-01",
                BookCopyStatus.Available),
            CreateCopy(
                3,
                "BS002-01",
                "S0002",
                "Clean Code",
                "Kệ 2-01",
                BookCopyStatus.Available),
            CreateCopy(
                4,
                "BS003-01",
                "S0003",
                "Chí Phèo",
                "Kệ 3-01",
                BookCopyStatus.Borrowed)
        ];

        public BookCopySearchRequest? LastRequest { get; private set; }

        public Task<PagedResult<BookCopyDto>> SearchAsync(
            BookCopySearchRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            string? keyword = request.Keyword?.Trim();
            BookCopyDto[] copies = Copies
                .Where(copy =>
                    (!request.Status.HasValue
                        || copy.Status == request.Status.Value)
                    && (string.IsNullOrWhiteSpace(keyword)
                        || copy.CopyCode.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase)
                        || copy.BookTitle.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase)
                        || (copy.ShelfLocation?.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase) ?? false)))
                .ToArray();
            return Task.FromResult(
                new PagedResult<BookCopyDto>(
                    copies,
                    copies.Length,
                    1,
                    request.PageSize));
        }

        private static BookCopyDto CreateCopy(
            int id,
            string copyCode,
            string bookCode,
            string bookTitle,
            string shelfLocation,
            BookCopyStatus status)
        {
            return new BookCopyDto(
                id,
                copyCode,
                id,
                bookCode,
                bookTitle,
                shelfLocation,
                DateOnly.FromDateTime(DateTime.Today).AddMonths(-6),
                PhysicalCondition.Good,
                status,
                null,
                DateTime.UtcNow,
                DateTime.UtcNow);
        }

        public Task<BookCopyDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<BookCopyBorrowHistoryDto>>
            GetBorrowHistoryAsync(
                int bookCopyId,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> CreateAsync(
            BookCopyUpsertRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> UpdateAsync(
            int id,
            BookCopyUpsertRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OperationResult> ChangeStatusAsync(
            int id,
            BookCopyStatus status,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class BorrowServiceStub : IBorrowService
    {
        public int? LastEligibilityReaderId { get; private set; }

        public Task<BorrowPolicyDto> GetBorrowPolicyAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BorrowPolicyDto(5, 14, 0m));
        }

        public Task<OperationResult> ValidateReaderEligibilityAsync(
            int readerId,
            CancellationToken cancellationToken = default)
        {
            LastEligibilityReaderId = readerId;
            return Task.FromResult(OperationResult.Success());
        }

        public Task<IReadOnlyList<BorrowSlipDetailDto>>
            GetReaderActiveBorrowsAsync(
                int readerId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<BorrowSlipDetailDto>>([]);
        }

        public Task<OperationResult> ValidateBorrowRequestAsync(
            BorrowCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public Task<OperationResult> CreateBorrowSlipAsync(
            BorrowCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public Task<BorrowSlipDto?> GetBorrowSlipAsync(
            int borrowSlipId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PagedResult<BorrowSlipListItemDto>>
            GetActiveBorrowSlipsAsync(
                BorrowSlipSearchRequest request,
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
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ConfirmAsync(
            string title,
            string message,
            string confirmText = "Xác nhận",
            string cancelText = "Hủy",
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
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
