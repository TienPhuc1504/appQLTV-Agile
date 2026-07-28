using FluentAssertions;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure;
using LibraryManagement.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Tests.Services;

public sealed class CatalogServiceTests
{
    [Fact]
    public async Task CategoryService_ShouldCreateSearchUpdateAndDeactivateCategory()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness =
            await CatalogServiceHarness.CreateAsync(cancellationToken: cancellationToken);
        ICategoryService service =
            harness.Provider.GetRequiredService<ICategoryService>();
        const string originalName = "Khoa học dữ liệu kiểm thử";
        const string updatedName = "Khoa học dữ liệu nâng cao";

        OperationResult createResult = await service.CreateAsync(
            new CategoryUpsertRequest(
                $"  {originalName}  ",
                "  Danh mục dùng cho kiểm thử.  "),
            cancellationToken);
        IReadOnlyList<CategoryDto> searchResult =
            await service.SearchAsync(
                "khoa học dữ liệu kiểm thử",
                includeInactive: true,
                cancellationToken: cancellationToken);
        CategoryDto created = searchResult.Single(
            category => category.Name == originalName);

        OperationResult updateResult = await service.UpdateAsync(
            created.Id,
            new CategoryUpsertRequest(updatedName, "Đã cập nhật."),
            cancellationToken);
        OperationResult deactivateResult =
            await service.SetActiveAsync(created.Id, false, cancellationToken);
        CategoryDto? deactivated =
            await service.GetByIdAsync(created.Id, cancellationToken);

        createResult.Succeeded.Should().BeTrue();
        updateResult.Succeeded.Should().BeTrue();
        deactivateResult.Succeeded.Should().BeTrue();
        deactivated.Should().NotBeNull();
        deactivated!.Name.Should().Be(updatedName);
        deactivated.IsActive.Should().BeFalse();
        (await service.SearchAsync(
                updatedName,
                cancellationToken: cancellationToken))
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task CategoryService_WithDuplicateNameIgnoringCase_ShouldFail()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness =
            await CatalogServiceHarness.CreateAsync(cancellationToken: cancellationToken);
        ICategoryService service =
            harness.Provider.GetRequiredService<ICategoryService>();

        OperationResult first = await service.CreateAsync(
            new CategoryUpsertRequest("Công nghệ lượng tử", null),
            cancellationToken);
        OperationResult duplicate = await service.CreateAsync(
            new CategoryUpsertRequest("công nghệ lượng tử", null),
            cancellationToken);

        first.Succeeded.Should().BeTrue();
        duplicate.Succeeded.Should().BeFalse();
        duplicate.ErrorMessage.Should().Contain("đã tồn tại");
    }

    [Fact]
    public async Task AuthorService_WithFutureDateOfBirth_ShouldFail()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness =
            await CatalogServiceHarness.CreateAsync(cancellationToken: cancellationToken);
        IAuthorService service =
            harness.Provider.GetRequiredService<IAuthorService>();

        OperationResult result = await service.CreateAsync(
            new AuthorUpsertRequest(
                "Tác giả tương lai",
                DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                "Việt Nam",
                null),
            cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không được lớn hơn ngày hiện tại");
    }

    [Fact]
    public async Task AuthorService_WithSameNameAndBirthDate_ShouldRejectDuplicate()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness =
            await CatalogServiceHarness.CreateAsync(cancellationToken: cancellationToken);
        IAuthorService service =
            harness.Provider.GetRequiredService<IAuthorService>();
        var birthDate = new DateOnly(1988, 4, 12);

        OperationResult first = await service.CreateAsync(
            new AuthorUpsertRequest(
                "Nguyễn Tác Giả Kiểm Thử",
                birthDate,
                "Việt Nam",
                null),
            cancellationToken);
        OperationResult duplicate = await service.CreateAsync(
            new AuthorUpsertRequest(
                "nguyễn tác giả kiểm thử",
                birthDate,
                "Việt Nam",
                null),
            cancellationToken);

        first.Succeeded.Should().BeTrue();
        duplicate.Succeeded.Should().BeFalse();
        duplicate.ErrorMessage.Should().Contain("đã tồn tại");
    }

    [Theory]
    [InlineData("khong-phai-email", "https://example.com", "Email")]
    [InlineData("contact@example.com", "ftp://example.com", "Website")]
    public async Task PublisherService_WithInvalidContactData_ShouldFail(
        string email,
        string website,
        string expectedMessage)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness =
            await CatalogServiceHarness.CreateAsync(cancellationToken: cancellationToken);
        IPublisherService service =
            harness.Provider.GetRequiredService<IPublisherService>();

        OperationResult result = await service.CreateAsync(
            new PublisherUpsertRequest(
                "Nhà xuất bản kiểm thử",
                null,
                "0901234567",
                email,
                website),
            cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain(expectedMessage);
    }

    [Fact]
    public async Task PublisherService_WithValidData_ShouldNormalizeAndPersist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness =
            await CatalogServiceHarness.CreateAsync(cancellationToken: cancellationToken);
        IPublisherService service =
            harness.Provider.GetRequiredService<IPublisherService>();
        const string publisherName = "Nhà xuất bản tích hợp";

        OperationResult result = await service.CreateAsync(
            new PublisherUpsertRequest(
                $"  {publisherName}  ",
                "  Thành phố Hồ Chí Minh  ",
                "0901 234 567",
                "contact@example.com",
                "https://example.com"),
            cancellationToken);
        PublisherDto publisher = (await service.SearchAsync(
                "tích hợp",
                cancellationToken: cancellationToken))
            .Single(item => item.Name == publisherName);

        result.Succeeded.Should().BeTrue();
        publisher.Address.Should().Be("Thành phố Hồ Chí Minh");
        publisher.Email.Should().Be("contact@example.com");
        publisher.Website.Should().Be("https://example.com");
    }

    [Fact]
    public async Task CategoryService_WithoutManageBooksPermission_ShouldDenyWrite()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness =
            await CatalogServiceHarness.CreateAsync(
                hasManageBooksPermission: false,
                cancellationToken: cancellationToken);
        ICategoryService service =
            harness.Provider.GetRequiredService<ICategoryService>();

        OperationResult result = await service.CreateAsync(
            new CategoryUpsertRequest("Không được phép", null),
            cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không có quyền");
    }

    private sealed class CatalogServiceHarness : IAsyncDisposable
    {
        private CatalogServiceHarness(
            ServiceProvider provider,
            string databasePath)
        {
            Provider = provider;
            DatabasePath = databasePath;
        }

        public ServiceProvider Provider { get; }

        private string DatabasePath { get; }

        public static async Task<CatalogServiceHarness> CreateAsync(
            bool hasManageBooksPermission = true,
            CancellationToken cancellationToken = default)
        {
            string databasePath = Path.Combine(
                Path.GetTempPath(),
                "LibraryManagement.Tests",
                $"{Guid.NewGuid():N}.db");
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:LibraryDatabase"] =
                            $"Data Source={databasePath};Foreign Keys=True",
                        ["Security:BCryptWorkFactor"] = "4",
                        ["Storage:LoginPreferencesFile"] =
                            Path.ChangeExtension(databasePath, ".json")
                    })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);
            services.AddSingleton<IAuthenticationService>(
                new PermissionAuthenticationService(hasManageBooksPermission));
            ServiceProvider provider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });

            IDbContextFactory<LibraryDbContext> dbContextFactory =
                provider.GetRequiredService<IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);

            return new CatalogServiceHarness(provider, databasePath);
        }

        public async ValueTask DisposeAsync()
        {
            await Provider.DisposeAsync();
            SqliteConnection.ClearAllPools();

            if (File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
            }

            string preferencePath = Path.ChangeExtension(DatabasePath, ".json");
            if (File.Exists(preferencePath))
            {
                File.Delete(preferencePath);
            }
        }
    }

    private sealed class PermissionAuthenticationService(bool isAllowed)
        : IAuthenticationService
    {
        public Task<AuthenticationResult> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                AuthenticationResult.Failure("Không sử dụng trong kiểm thử."));
        }

        public void Logout()
        {
        }

        public Task<OperationResult> ChangePasswordAsync(
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public Task<OperationResult> ResetPasswordAsync(
            int employeeId,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public CurrentUser? GetCurrentUser() => null;

        public bool CheckPermission(Permission permission)
        {
            return isAllowed && permission == Permission.ManageBooks;
        }
    }
}
