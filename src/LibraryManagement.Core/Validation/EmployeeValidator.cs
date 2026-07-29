using LibraryManagement.Core.DTOs;

namespace LibraryManagement.Core.Validation;

public static class EmployeeValidator
{
    public static ValidatedEmployeeInput Validate(
        EmployeeUpsertRequest request,
        bool requirePassword,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.Gender))
        {
            throw new DomainValidationException("Giới tính không hợp lệ.");
        }

        if (request.RoleId <= 0)
        {
            throw new DomainValidationException("Vui lòng chọn vai trò.");
        }

        string employeeCode = DomainValidator.MaximumLength(
            DomainValidator.Required(request.EmployeeCode, "mã nhân viên"),
            20,
            "Mã nhân viên");
        string fullName = DomainValidator.MaximumLength(
            DomainValidator.Required(request.FullName, "tên nhân viên"),
            150,
            "Tên nhân viên");
        DateOnly? dateOfBirth = DomainValidator.NotInFuture(
            request.DateOfBirth,
            "Ngày sinh",
            today);
        string username = CredentialValidator.NormalizeUsername(
            request.Username);
        string? password = requirePassword
            ? CredentialValidator.ValidateNewPassword(request.InitialPassword)
            : null;

        return new ValidatedEmployeeInput(
            employeeCode,
            fullName,
            dateOfBirth,
            request.Gender,
            DomainValidator.OptionalPhoneNumber(request.PhoneNumber),
            DomainValidator.OptionalEmail(request.Email),
            DomainValidator.OptionalMaximumLength(
                request.Address,
                500,
                "Địa chỉ"),
            username,
            request.RoleId,
            password);
    }
}

public sealed record ValidatedEmployeeInput(
    string EmployeeCode,
    string FullName,
    DateOnly? DateOfBirth,
    Core.Enums.Gender Gender,
    string? PhoneNumber,
    string? Email,
    string? Address,
    string Username,
    int RoleId,
    string? Password);
