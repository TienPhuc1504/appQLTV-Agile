using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.Models;

public sealed record ReturnPersistenceCommand(
    IReadOnlyCollection<ReturnPersistenceItem> Items,
    DateOnly ReturnDate);

public sealed record ReturnPersistenceItem(
    int BorrowSlipDetailId,
    PhysicalCondition ReturnedCondition,
    BorrowSlipDetailStatus DetailStatus,
    BookCopyStatus BookCopyStatus,
    string? Notes,
    ReturnRecord ReturnRecord,
    IReadOnlyCollection<Fine> Fines);
