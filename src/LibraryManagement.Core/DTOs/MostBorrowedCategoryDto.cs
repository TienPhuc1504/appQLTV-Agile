namespace LibraryManagement.Core.DTOs;

public sealed record MostBorrowedCategoryDto(
    int CategoryId,
    string CategoryName,
    int BorrowCount);
