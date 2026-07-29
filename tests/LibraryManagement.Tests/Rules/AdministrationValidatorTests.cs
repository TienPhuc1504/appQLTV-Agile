using FluentAssertions;
using LibraryManagement.Core.Constants;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Validation;

namespace LibraryManagement.Tests.Rules;

public sealed class AdministrationValidatorTests
{
    [Fact]
    public void EmployeeValidator_WithFutureBirthDate_ShouldFail()
    {
        var request = new EmployeeUpsertRequest(
            "NV0100",
            "Nhân viên",
            new DateOnly(2027, 1, 1),
            Gender.Other,
            null,
            null,
            null,
            "employee100",
            2,
            "Employee@123");

        Action action = () => EmployeeValidator.Validate(
            request,
            requirePassword: true,
            new DateOnly(2026, 7, 29));

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("Ngày sinh không được lớn hơn ngày hiện tại.");
    }

    [Fact]
    public void SystemSettingValidator_WithInvariantDecimal_ShouldNormalize()
    {
        SystemSettingUpdateRequest result = SystemSettingValidator.Validate(
            new SystemSettingUpdateRequest(
                SystemSettingKeys.LostBookFineMultiplier,
                "2.50"));

        result.Value.Should().Be("2.5");
    }

    [Fact]
    public void SystemSettingValidator_WithVietnameseDecimal_ShouldNormalize()
    {
        SystemSettingUpdateRequest result = SystemSettingValidator.Validate(
            new SystemSettingUpdateRequest(
                SystemSettingKeys.DamagedBookFineMultiplier,
                "0,5"));

        result.Value.Should().Be("0.5");
    }

    [Fact]
    public void SystemSettingValidator_WithAmbiguousDecimal_ShouldFail()
    {
        Action action = () => SystemSettingValidator.Validate(
            new SystemSettingUpdateRequest(
                SystemSettingKeys.DamagedBookFineMultiplier,
                "1.000,5"));

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void SystemSettingValidator_WithUnknownKey_ShouldFail()
    {
        Action action = () => SystemSettingValidator.Validate(
            new SystemSettingUpdateRequest("UnknownSetting", "1"));

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("Cài đặt hệ thống không được hỗ trợ.");
    }
}
