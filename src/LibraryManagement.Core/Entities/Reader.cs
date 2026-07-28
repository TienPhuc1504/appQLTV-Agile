using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class Reader : AuditableEntity
{
    public string ReaderCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public ReaderType ReaderType { get; set; }

    public DateOnly RegisteredAt { get; set; }

    public DateOnly ExpirationDate { get; set; }

    public string? AvatarPath { get; set; }

    public ReaderStatus Status { get; set; }

    public string? Notes { get; set; }

    public ICollection<BorrowSlip> BorrowSlips { get; set; } = [];

    public ICollection<Fine> Fines { get; set; } = [];

    public bool IsCardValid(DateOnly date) =>
        Status == ReaderStatus.Active && ExpirationDate >= date;
}
