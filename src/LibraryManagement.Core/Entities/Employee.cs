using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class Employee : AuditableEntity
{
    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public Role Role { get; set; } = null!;

    public ICollection<BorrowSlip> BorrowSlips { get; set; } = [];

    public ICollection<ReturnRecord> ReturnRecords { get; set; } = [];

    public ICollection<Fine> CreatedFines { get; set; } = [];

    public ICollection<FinePayment> FinePayments { get; set; } = [];

    public ICollection<SystemSetting> UpdatedSystemSettings { get; set; } = [];

    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
