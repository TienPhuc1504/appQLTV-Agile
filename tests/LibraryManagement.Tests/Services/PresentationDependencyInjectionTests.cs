using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using LibraryManagement.App;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Navigation;
using LibraryManagement.App.Notifications;
using LibraryManagement.App.Services;
using LibraryManagement.App.Themes;
using LibraryManagement.App.ViewModels;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui;

namespace LibraryManagement.Tests.Services;

public sealed class PresentationDependencyInjectionTests
{
    [Fact]
    public void AddPresentation_ShouldRegisterAValidServiceGraph()
    {
        using ServiceProvider provider = CreateServiceProvider();

        provider.GetRequiredService<IAppNavigationService>()
            .Should()
            .BeSameAs(provider.GetRequiredService<IAppNavigationService>());
        provider.GetRequiredService<ISnackbarService>()
            .Should()
            .BeOfType<SnackbarService>();
        provider.GetRequiredService<IContentDialogService>()
            .Should()
            .BeOfType<ContentDialogService>();
        provider.GetRequiredService<IAppDialogService>()
            .Should()
            .BeOfType<AppDialogService>();
        provider.GetRequiredService<IAppNotificationService>()
            .Should()
            .BeOfType<AppNotificationService>();
        provider.GetRequiredService<IAppThemeService>()
            .Should()
            .BeOfType<AppThemeService>();
        provider.GetRequiredService<IDatabaseFilePickerService>()
            .Should()
            .BeOfType<DatabaseFilePickerService>();
        provider.GetRequiredService<MainViewModel>()
            .Should()
            .BeSameAs(provider.GetRequiredService<MainViewModel>());
        provider.GetRequiredService<FoundationViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<FoundationViewModel>());
        provider.GetRequiredService<CategoryViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<CategoryViewModel>());
        provider.GetRequiredService<AuthorViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<AuthorViewModel>());
        provider.GetRequiredService<PublisherViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<PublisherViewModel>());
        provider.GetRequiredService<BookViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<BookViewModel>());
        provider.GetRequiredService<BookCopyViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<BookCopyViewModel>());
        provider.GetRequiredService<ReaderViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<ReaderViewModel>());
        provider.GetRequiredService<BorrowViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<BorrowViewModel>());
        provider.GetRequiredService<ReturnViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<ReturnViewModel>());
        provider.GetRequiredService<FineViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<FineViewModel>());
        provider.GetRequiredService<DashboardViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<DashboardViewModel>());
        provider.GetRequiredService<EmployeeViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<EmployeeViewModel>());
        provider.GetRequiredService<SettingsViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<SettingsViewModel>());
        provider.GetRequiredService<ActivityLogViewModel>()
            .Should()
            .NotBeSameAs(provider.GetRequiredService<ActivityLogViewModel>());
    }

    [Fact]
    public void CatalogViewModels_WhenSelectionIsCleared_ShouldClearEditorFields()
    {
        using ServiceProvider provider = CreateServiceProvider();
        DateTime timestamp = DateTime.UtcNow;

        CategoryViewModel categoryViewModel =
            provider.GetRequiredService<CategoryViewModel>();
        categoryViewModel.SelectedItem = new CategoryDto(
            1,
            "Văn học",
            "Mô tả",
            true,
            timestamp,
            timestamp);
        categoryViewModel.SelectedItem = null;

        AuthorViewModel authorViewModel =
            provider.GetRequiredService<AuthorViewModel>();
        authorViewModel.SelectedItem = new AuthorDto(
            1,
            "Nguyễn Văn A",
            new DateOnly(1980, 1, 1),
            "Việt Nam",
            "Tiểu sử",
            true,
            timestamp,
            timestamp);
        authorViewModel.SelectedItem = null;

        PublisherViewModel publisherViewModel =
            provider.GetRequiredService<PublisherViewModel>();
        publisherViewModel.SelectedItem = new PublisherDto(
            1,
            "Nhà xuất bản A",
            "Địa chỉ",
            "0901234567",
            "contact@example.com",
            "https://example.com",
            true,
            timestamp,
            timestamp);
        publisherViewModel.SelectedItem = null;

        categoryViewModel.Name.Should().BeEmpty();
        categoryViewModel.Description.Should().BeNull();
        authorViewModel.FullName.Should().BeEmpty();
        authorViewModel.DateOfBirth.Should().BeNull();
        authorViewModel.Nationality.Should().BeNull();
        authorViewModel.Biography.Should().BeNull();
        publisherViewModel.Name.Should().BeEmpty();
        publisherViewModel.Address.Should().BeNull();
        publisherViewModel.PhoneNumber.Should().BeNull();
        publisherViewModel.Email.Should().BeNull();
        publisherViewModel.Website.Should().BeNull();
    }

    [Fact]
    public void CatalogViewModels_WithInvalidInput_ShouldExposeVietnameseValidationMessages()
    {
        using ServiceProvider provider = CreateServiceProvider();
        CategoryViewModel categoryViewModel =
            provider.GetRequiredService<CategoryViewModel>();
        AuthorViewModel authorViewModel =
            provider.GetRequiredService<AuthorViewModel>();
        PublisherViewModel publisherViewModel =
            provider.GetRequiredService<PublisherViewModel>();
        ReaderViewModel readerViewModel =
            provider.GetRequiredService<ReaderViewModel>();

        categoryViewModel.Name = string.Empty;
        authorViewModel.DateOfBirth = DateTime.Today.AddDays(1);
        publisherViewModel.Email = "email-khong-hop-le";
        readerViewModel.FullName = string.Empty;
        readerViewModel.Email = "email-khong-hop-le";

        categoryViewModel.Validate().Should().BeFalse();
        authorViewModel.Validate().Should().BeFalse();
        publisherViewModel.Validate().Should().BeFalse();
        readerViewModel.Validate().Should().BeFalse();
        GetFirstValidationMessage(categoryViewModel, nameof(CategoryViewModel.Name))
            .Should()
            .Be("Vui lòng nhập tên thể loại.");
        GetFirstValidationMessage(
                authorViewModel,
                nameof(AuthorViewModel.DateOfBirth))
            .Should()
            .Be("Ngày sinh không được lớn hơn ngày hiện tại.");
        GetFirstValidationMessage(
                publisherViewModel,
                nameof(PublisherViewModel.Email))
            .Should()
            .Be("Email không đúng định dạng.");
        GetFirstValidationMessage(
                readerViewModel,
                nameof(ReaderViewModel.FullName))
            .Should()
            .Be("Vui lòng nhập tên độc giả.");
        GetFirstValidationMessage(
                readerViewModel,
                nameof(ReaderViewModel.Email))
            .Should()
            .Be("Email không đúng định dạng.");
    }

    [Fact]
    public void AddPresentation_WhenServicesIsNull_ShouldThrow()
    {
        IServiceCollection services = null!;

        Action action = () => services.AddPresentation();

        action.Should().Throw<ArgumentNullException>();
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(CreateConfiguration());
        services.AddPresentation();
        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    private static string? GetFirstValidationMessage(
        BaseViewModel viewModel,
        string propertyName)
    {
        return viewModel.GetErrors(propertyName)
            .OfType<ValidationResult>()
            .Select(result => result.ErrorMessage)
            .FirstOrDefault();
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LibraryDatabase"] =
                        $"Data Source={Path.Combine(Path.GetTempPath(), "LibraryManagement.Tests", "presentation-library.db")};Foreign Keys=True",
                    ["Security:BCryptWorkFactor"] = "4",
                    ["Storage:LoginPreferencesFile"] =
                        Path.Combine(
                            Path.GetTempPath(),
                            "LibraryManagement.Tests",
                            "presentation-login-preferences.json"),
                    ["Storage:BookCoversDirectory"] =
                        Path.Combine(
                            Path.GetTempPath(),
                            "LibraryManagement.Tests",
                            "BookCovers")
                })
            .Build();
    }
}
