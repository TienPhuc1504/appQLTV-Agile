namespace LibraryManagement.Core.Models;

public sealed record CurrentUser(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string Username,
    string RoleName);
