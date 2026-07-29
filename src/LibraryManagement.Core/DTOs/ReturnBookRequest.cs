using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record ReturnBookRequest(
    int BorrowSlipDetailId,
    PhysicalCondition ReturnedCondition,
    string? Notes = null);

public sealed record ReturnMultipleBooksRequest(
    IReadOnlyCollection<ReturnBookRequest> Items,
    DateOnly ReturnDate);
