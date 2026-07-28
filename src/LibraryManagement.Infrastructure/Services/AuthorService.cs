using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed class AuthorService(
    IAuthorRepository authorRepository,
    IAuthenticationService authenticationService,
    ILogger<AuthorService> logger)
    : IAuthorService
{
    public Task<IReadOnlyList<AuthorDto>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(null, includeInactive, cancellationToken);
    }

    public async Task<IReadOnlyList<AuthorDto>> SearchAsync(
        string? keyword,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        IReadOnlyList<Author> authors =
            await authorRepository.SearchAsync(
                keyword,
                includeInactive,
                cancellationToken);
        return authors.Select(Map).ToArray();
    }

    public async Task<AuthorDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        if (id <= 0)
        {
            return null;
        }

        Author? author = await authorRepository.GetByIdAsync(id, cancellationToken);
        return author is null ? null : Map(author);
    }

    public async Task<OperationResult> CreateAsync(
        AuthorUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            CatalogServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        try
        {
            AuthorInput input = Validate(request);
            if (await authorRepository.ExistsAsync(
                    input.FullName,
                    input.DateOfBirth,
                    cancellationToken: cancellationToken))
            {
                return OperationResult.Failure(
                    "Tác giả có cùng họ tên và ngày sinh đã tồn tại.");
            }

            var author = new Author
            {
                FullName = input.FullName,
                DateOfBirth = input.DateOfBirth,
                Nationality = input.Nationality,
                Biography = input.Biography,
                IsActive = input.IsActive
            };
            await authorRepository.AddAsync(author, cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Không thể tạo tác giả {AuthorName}.", request.FullName);
            return OperationResult.Failure("Không thể lưu thông tin tác giả.");
        }
    }

    public async Task<OperationResult> UpdateAsync(
        int id,
        AuthorUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            CatalogServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        if (id <= 0)
        {
            return OperationResult.Failure("Tác giả không tồn tại.");
        }

        try
        {
            AuthorInput input = Validate(request);
            Author? author = await authorRepository.GetByIdAsync(id, cancellationToken);
            if (author is null)
            {
                return OperationResult.Failure("Tác giả không tồn tại.");
            }

            if (await authorRepository.ExistsAsync(
                    input.FullName,
                    input.DateOfBirth,
                    id,
                    cancellationToken))
            {
                return OperationResult.Failure(
                    "Tác giả có cùng họ tên và ngày sinh đã tồn tại.");
            }

            author.FullName = input.FullName;
            author.DateOfBirth = input.DateOfBirth;
            author.Nationality = input.Nationality;
            author.Biography = input.Biography;
            author.IsActive = input.IsActive;
            await authorRepository.UpdateAsync(author, cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Không thể cập nhật tác giả có mã {AuthorId}.",
                id);
            return OperationResult.Failure("Không thể cập nhật thông tin tác giả.");
        }
    }

    public async Task<OperationResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        OperationResult? accessFailure =
            CatalogServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        Author? author = await authorRepository.GetByIdAsync(id, cancellationToken);
        if (author is null)
        {
            return OperationResult.Failure("Tác giả không tồn tại.");
        }

        try
        {
            author.IsActive = isActive;
            await authorRepository.UpdateAsync(author, cancellationToken);
            return OperationResult.Success();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Không thể đổi trạng thái tác giả có mã {AuthorId}.",
                id);
            return OperationResult.Failure(
                "Không thể cập nhật trạng thái tác giả.");
        }
    }

    private static AuthorInput Validate(AuthorUpsertRequest request)
    {
        string fullName = DomainValidator.MaximumLength(
            DomainValidator.Required(request.FullName, "họ tên tác giả"),
            150,
            "Họ tên tác giả");
        DateOnly? dateOfBirth = DomainValidator.NotInFuture(
            request.DateOfBirth,
            "Ngày sinh",
            DateOnly.FromDateTime(DateTime.Today));
        string? nationality = DomainValidator.OptionalMaximumLength(
            request.Nationality,
            100,
            "Quốc tịch");
        string? biography = DomainValidator.OptionalMaximumLength(
            request.Biography,
            4000,
            "Tiểu sử");
        return new AuthorInput(
            fullName,
            dateOfBirth,
            nationality,
            biography,
            request.IsActive);
    }

    private static AuthorDto Map(Author author)
    {
        return new AuthorDto(
            author.Id,
            author.FullName,
            author.DateOfBirth,
            author.Nationality,
            author.Biography,
            author.IsActive,
            author.CreatedAt,
            author.UpdatedAt);
    }

    private sealed record AuthorInput(
        string FullName,
        DateOnly? DateOfBirth,
        string? Nationality,
        string? Biography,
        bool IsActive);
}
