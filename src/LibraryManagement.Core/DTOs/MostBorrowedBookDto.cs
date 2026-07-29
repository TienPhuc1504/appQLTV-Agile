namespace LibraryManagement.Core.DTOs;

public sealed record MostBorrowedBookDto(
    int BookId,
    string BookCode,
    string Title,
    int BorrowCount);
