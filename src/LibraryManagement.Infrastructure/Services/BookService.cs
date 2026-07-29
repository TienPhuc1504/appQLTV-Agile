using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed class BookService(
    IBookRepository bookRepository,
    IBookCoverStorageService coverStorageService,
    IAuthenticationService authenticationService,
    ILogger<BookService> logger)
    : IBookService
{
    private static readonly int[] AllowedPageSizes = [10, 20, 50, 100];

    public async Task<PagedResult<BookListItemDto>> SearchAsync(
        BookSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        int pageNumber = Math.Max(1, request.PageNumber);
        int pageSize = AllowedPageSizes.Contains(request.PageSize)
            ? request.PageSize
            : 20;
        BookSearchRequest normalizedRequest = request with
        {
            Keyword = DomainValidator.OptionalMaximumLength(
                request.Keyword,
                300,
                "Từ khóa"),
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        PagedResult<Book> result = await bookRepository.SearchAsync(
            normalizedRequest,
            cancellationToken);
        return new PagedResult<BookListItemDto>(
            result.Items.Select(MapListItem).ToArray(),
            result.TotalCount,
            result.PageNumber,
            result.PageSize);
    }

    public async Task<BookDetailDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        Book? book = id <= 0
            ? null
            : await bookRepository.GetByIdAsync(id, cancellationToken);
        return book is null ? null : MapDetail(book);
    }

    public async Task<IReadOnlyList<BookCopyDto>> GetAvailableCopiesAsync(
        int bookId,
        CancellationToken cancellationToken = default)
    {
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        Book? book = bookId <= 0
            ? null
            : await bookRepository.GetByIdAsync(bookId, cancellationToken);
        if (book is null)
        {
            return [];
        }

        return book.BookCopies
            .Where(copy => copy.Status == BookCopyStatus.Available)
            .OrderBy(copy => copy.CopyCode)
            .Select(copy => new BookCopyDto(
                copy.Id,
                copy.CopyCode,
                book.Id,
                book.BookCode,
                book.Title,
                copy.ShelfLocation,
                copy.ImportedAt,
                copy.PhysicalCondition,
                copy.Status,
                copy.Notes,
                copy.CreatedAt,
                copy.UpdatedAt))
            .ToArray();
    }

    public async Task<OperationResult> CreateAsync(
        BookUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            CatalogServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        string? storedCoverPath = null;
        try
        {
            BookInput input = await ValidateAsync(request, null, cancellationToken);
            if (input.CoverImageSourcePath is not null)
            {
                storedCoverPath = await coverStorageService.SaveAsync(
                    input.CoverImageSourcePath,
                    cancellationToken);
            }

            var book = new Book
            {
                BookCode = input.BookCode,
                ISBN = input.ISBN,
                Title = input.Title,
                PublisherId = input.PublisherId,
                PublicationYear = input.PublicationYear,
                Language = input.Language,
                PageCount = input.PageCount,
                Price = input.Price,
                CoverImagePath = storedCoverPath,
                Description = input.Description,
                IsActive = input.IsActive,
                BookAuthors = input.AuthorIds
                    .Select(id => new BookAuthor { AuthorId = id })
                    .ToList(),
                BookCategories = input.CategoryIds
                    .Select(id => new BookCategory { CategoryId = id })
                    .ToList()
            };
            await bookRepository.AddAsync(book, cancellationToken);
            return OperationResult.Success();
        }
        catch (OperationCanceledException)
        {
            await DeleteCoverIfNeededAsync(storedCoverPath);
            throw;
        }
        catch (Exception exception) when (
            exception is DomainValidationException
                or FileNotFoundException
                or InvalidOperationException)
        {
            await DeleteCoverIfNeededAsync(storedCoverPath);
            return OperationResult.Failure(exception.Message);
        }
        catch (DbUpdateException exception)
        {
            await DeleteCoverIfNeededAsync(storedCoverPath);
            logger.LogError(exception, "Không thể tạo sách {BookCode}.", request.BookCode);
            return OperationResult.Failure(
                "Không thể lưu sách. Vui lòng kiểm tra mã sách hoặc ISBN trùng lặp.");
        }
    }

    public async Task<OperationResult> UpdateAsync(
        int id,
        BookUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            CatalogServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        Book? existing = id <= 0
            ? null
            : await bookRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return OperationResult.Failure("Sách không tồn tại.");
        }

        string? newCoverPath = null;
        try
        {
            BookInput input = await ValidateAsync(request, id, cancellationToken);
            if (input.CoverImageSourcePath is not null)
            {
                newCoverPath = await coverStorageService.SaveAsync(
                    input.CoverImageSourcePath,
                    cancellationToken);
            }

            string? oldCoverPath = existing.CoverImagePath;
            existing.BookCode = input.BookCode;
            existing.ISBN = input.ISBN;
            existing.Title = input.Title;
            existing.PublisherId = input.PublisherId;
            existing.PublicationYear = input.PublicationYear;
            existing.Language = input.Language;
            existing.PageCount = input.PageCount;
            existing.Price = input.Price;
            existing.CoverImagePath = newCoverPath ?? oldCoverPath;
            existing.Description = input.Description;
            existing.IsActive = input.IsActive;
            await bookRepository.UpdateAsync(
                existing,
                input.AuthorIds,
                input.CategoryIds,
                cancellationToken);

            if (newCoverPath is not null && oldCoverPath is not null)
            {
                await coverStorageService.DeleteAsync(oldCoverPath, CancellationToken.None);
            }

            return OperationResult.Success();
        }
        catch (OperationCanceledException)
        {
            await DeleteCoverIfNeededAsync(newCoverPath);
            throw;
        }
        catch (Exception exception) when (
            exception is DomainValidationException
                or FileNotFoundException
                or InvalidOperationException)
        {
            await DeleteCoverIfNeededAsync(newCoverPath);
            return OperationResult.Failure(exception.Message);
        }
        catch (DbUpdateException exception)
        {
            await DeleteCoverIfNeededAsync(newCoverPath);
            logger.LogError(
                exception,
                "Không thể cập nhật sách có mã {BookId}.",
                id);
            return OperationResult.Failure(
                "Không thể cập nhật sách. Vui lòng kiểm tra dữ liệu trùng lặp.");
        }
    }

    public async Task<OperationResult> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        OperationResult? accessFailure =
            CatalogServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        Book? book = id <= 0
            ? null
            : await bookRepository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return OperationResult.Failure("Sách không tồn tại.");
        }

        book.IsActive = false;
        await bookRepository.UpdateAsync(
            book,
            book.BookAuthors.Select(item => item.AuthorId).ToArray(),
            book.BookCategories.Select(item => item.CategoryId).ToArray(),
            cancellationToken);
        return OperationResult.Success();
    }

    private async Task<BookInput> ValidateAsync(
        BookUpsertRequest request,
        int? excludingId,
        CancellationToken cancellationToken)
    {
        string bookCode = BookValidator.BookCode(request.BookCode);
        string? isbn = BookValidator.Isbn(request.ISBN);
        string title = DomainValidator.MaximumLength(
            DomainValidator.Required(request.Title, "tên sách"),
            300,
            "Tên sách");
        string? language = DomainValidator.OptionalMaximumLength(
            request.Language,
            50,
            "Ngôn ngữ");
        string? description = DomainValidator.OptionalMaximumLength(
            request.Description,
            4000,
            "Mô tả");
        int publicationYear = BookValidator.PublicationYear(
            request.PublicationYear,
            DateTime.Today.Year);
        int pageCount = BookValidator.Positive(request.PageCount, "Số trang");
        decimal price = DomainValidator.NonNegative(request.Price, "Giá sách");
        int[] authorIds = request.AuthorIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        int[] categoryIds = request.CategoryIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        if (request.PublisherId <= 0)
        {
            throw new DomainValidationException("Vui lòng chọn nhà xuất bản.");
        }

        if (authorIds.Length == 0)
        {
            throw new DomainValidationException("Vui lòng chọn ít nhất một tác giả.");
        }

        if (categoryIds.Length == 0)
        {
            throw new DomainValidationException("Vui lòng chọn ít nhất một thể loại.");
        }

        if (await bookRepository.BookCodeExistsAsync(
                bookCode,
                excludingId,
                cancellationToken))
        {
            throw new DomainValidationException("Mã sách đã tồn tại.");
        }

        if (isbn is not null
            && await bookRepository.IsbnExistsAsync(
                isbn,
                excludingId,
                cancellationToken))
        {
            throw new DomainValidationException("ISBN đã tồn tại.");
        }

        if (!await bookRepository.ReferenceDataExistsAsync(
                request.PublisherId,
                authorIds,
                categoryIds,
                cancellationToken))
        {
            throw new DomainValidationException(
                "Nhà xuất bản, tác giả hoặc thể loại không tồn tại hoặc đã ngừng sử dụng.");
        }

        return new BookInput(
            bookCode,
            isbn,
            title,
            request.PublisherId,
            publicationYear,
            language,
            pageCount,
            price,
            DomainValidator.Optional(request.CoverImageSourcePath),
            description,
            authorIds,
            categoryIds,
            request.IsActive);
    }

    private async Task DeleteCoverIfNeededAsync(string? path)
    {
        if (path is null)
        {
            return;
        }

        try
        {
            await coverStorageService.DeleteAsync(path, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể xóa ảnh bìa tạm {CoverPath}.", path);
        }
    }

    private static BookListItemDto MapListItem(Book book)
    {
        return new BookListItemDto(
            book.Id,
            book.BookCode,
            book.ISBN,
            book.Title,
            book.Publisher.Name,
            book.PublicationYear,
            book.Price,
            book.CoverImagePath,
            book.IsActive,
            book.BookCopies.Count,
            book.BookCopies.Count(copy => copy.Status == BookCopyStatus.Available),
            string.Join(", ", book.BookAuthors.Select(item => item.Author.FullName)),
            string.Join(", ", book.BookCategories.Select(item => item.Category.Name)));
    }

    private static BookDetailDto MapDetail(Book book)
    {
        return new BookDetailDto(
            book.Id,
            book.BookCode,
            book.ISBN,
            book.Title,
            book.PublisherId,
            book.Publisher.Name,
            book.PublicationYear,
            book.Language,
            book.PageCount,
            book.Price,
            book.CoverImagePath,
            book.Description,
            book.IsActive,
            book.BookAuthors.Select(item => item.AuthorId).ToArray(),
            book.BookAuthors.Select(item => item.Author.FullName).ToArray(),
            book.BookCategories.Select(item => item.CategoryId).ToArray(),
            book.BookCategories.Select(item => item.Category.Name).ToArray(),
            book.BookCopies.Count,
            book.BookCopies.Count(copy => copy.Status == BookCopyStatus.Available),
            book.CreatedAt,
            book.UpdatedAt);
    }

    private sealed record BookInput(
        string BookCode,
        string? ISBN,
        string Title,
        int PublisherId,
        int PublicationYear,
        string? Language,
        int PageCount,
        decimal Price,
        string? CoverImageSourcePath,
        string? Description,
        IReadOnlyCollection<int> AuthorIds,
        IReadOnlyCollection<int> CategoryIds,
        bool IsActive);
}
