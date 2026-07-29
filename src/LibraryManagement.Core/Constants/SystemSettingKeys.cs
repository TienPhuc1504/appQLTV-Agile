namespace LibraryManagement.Core.Constants;

public static class SystemSettingKeys
{
    public const string MaximumBorrowedBooks = nameof(MaximumBorrowedBooks);

    public const string DefaultBorrowDays = nameof(DefaultBorrowDays);

    public const string MaximumRenewalCount = nameof(MaximumRenewalCount);

    public const string RenewalDays = nameof(RenewalDays);

    public const string OverdueFinePerDay = nameof(OverdueFinePerDay);

    public const string LostBookFineMultiplier = nameof(LostBookFineMultiplier);

    public const string DamagedBookFineMultiplier =
        nameof(DamagedBookFineMultiplier);

    public const string ReaderCardValidityMonths =
        nameof(ReaderCardValidityMonths);

    public const string MaximumOutstandingFineAmount =
        nameof(MaximumOutstandingFineAmount);
}
