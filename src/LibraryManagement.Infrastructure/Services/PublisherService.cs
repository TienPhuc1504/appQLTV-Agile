using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed class PublisherService(
    IPublisherRepository publisherRepository,
    IAuthenticationService authenticationService,
    ILogger<PublisherService> logger)
    : IPublisherService
{
    public Task<IReadOnlyList<PublisherDto>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(null, includeInactive, cancellationToken);
    }

    public async Task<IReadOnlyList<PublisherDto>> SearchAsync(
        string? keyword,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        IReadOnlyList<Publisher> publishers =
            await publisherRepository.SearchAsync(
                keyword,
                includeInactive,
                cancellationToken);
        return publishers.Select(Map).ToArray();
    }

    public async Task<PublisherDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        if (id <= 0)
        {
            return null;
        }

        Publisher? publisher =
            await publisherRepository.GetByIdAsync(id, cancellationToken);
        return publisher is null ? null : Map(publisher);
    }

    public async Task<OperationResult> CreateAsync(
        PublisherUpsertRequest request,
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
            PublisherInput input = Validate(request);
            if (await publisherRepository.NameExistsAsync(
                    input.Name,
                    cancellationToken: cancellationToken))
            {
                return OperationResult.Failure("Tên nhà xuất bản đã tồn tại.");
            }

            var publisher = new Publisher
            {
                Name = input.Name,
                Address = input.Address,
                PhoneNumber = input.PhoneNumber,
                Email = input.Email,
                Website = input.Website,
                IsActive = input.IsActive
            };
            await publisherRepository.AddAsync(publisher, cancellationToken);
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
                "Không thể tạo nhà xuất bản {PublisherName}.",
                request.Name);
            return OperationResult.Failure(
                "Không thể lưu nhà xuất bản. Vui lòng kiểm tra dữ liệu trùng lặp.");
        }
    }

    public async Task<OperationResult> UpdateAsync(
        int id,
        PublisherUpsertRequest request,
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
            return OperationResult.Failure("Nhà xuất bản không tồn tại.");
        }

        try
        {
            PublisherInput input = Validate(request);
            Publisher? publisher =
                await publisherRepository.GetByIdAsync(id, cancellationToken);
            if (publisher is null)
            {
                return OperationResult.Failure("Nhà xuất bản không tồn tại.");
            }

            if (await publisherRepository.NameExistsAsync(
                    input.Name,
                    id,
                    cancellationToken))
            {
                return OperationResult.Failure("Tên nhà xuất bản đã tồn tại.");
            }

            publisher.Name = input.Name;
            publisher.Address = input.Address;
            publisher.PhoneNumber = input.PhoneNumber;
            publisher.Email = input.Email;
            publisher.Website = input.Website;
            publisher.IsActive = input.IsActive;
            await publisherRepository.UpdateAsync(publisher, cancellationToken);
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
                "Không thể cập nhật nhà xuất bản có mã {PublisherId}.",
                id);
            return OperationResult.Failure(
                "Không thể cập nhật thông tin nhà xuất bản.");
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

        Publisher? publisher =
            await publisherRepository.GetByIdAsync(id, cancellationToken);
        if (publisher is null)
        {
            return OperationResult.Failure("Nhà xuất bản không tồn tại.");
        }

        try
        {
            publisher.IsActive = isActive;
            await publisherRepository.UpdateAsync(publisher, cancellationToken);
            return OperationResult.Success();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Không thể đổi trạng thái nhà xuất bản có mã {PublisherId}.",
                id);
            return OperationResult.Failure(
                "Không thể cập nhật trạng thái nhà xuất bản.");
        }
    }

    private static PublisherInput Validate(PublisherUpsertRequest request)
    {
        string name = DomainValidator.MaximumLength(
            DomainValidator.Required(request.Name, "tên nhà xuất bản"),
            200,
            "Tên nhà xuất bản");
        string? address = DomainValidator.OptionalMaximumLength(
            request.Address,
            500,
            "Địa chỉ");
        string? phoneNumber = DomainValidator.OptionalPhoneNumber(
            request.PhoneNumber);
        string? email = DomainValidator.OptionalEmail(request.Email);
        string? website = DomainValidator.OptionalWebsite(request.Website);

        if (phoneNumber is not null)
        {
            DomainValidator.MaximumLength(phoneNumber, 20, "Số điện thoại");
        }

        if (email is not null)
        {
            DomainValidator.MaximumLength(email, 254, "Email");
        }

        if (website is not null)
        {
            DomainValidator.MaximumLength(website, 300, "Website");
        }

        return new PublisherInput(
            name,
            address,
            phoneNumber,
            email,
            website,
            request.IsActive);
    }

    private static PublisherDto Map(Publisher publisher)
    {
        return new PublisherDto(
            publisher.Id,
            publisher.Name,
            publisher.Address,
            publisher.PhoneNumber,
            publisher.Email,
            publisher.Website,
            publisher.IsActive,
            publisher.CreatedAt,
            publisher.UpdatedAt);
    }

    private sealed record PublisherInput(
        string Name,
        string? Address,
        string? PhoneNumber,
        string? Email,
        string? Website,
        bool IsActive);
}
