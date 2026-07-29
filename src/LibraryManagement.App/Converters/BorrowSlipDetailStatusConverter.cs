using System.Globalization;
using System.Windows.Data;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.App.Converters;

public sealed class BorrowSlipDetailStatusConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is BorrowSlipDetailStatus status
            ? status switch
            {
                BorrowSlipDetailStatus.Borrowing => "Đang mượn",
                BorrowSlipDetailStatus.Returned => "Đã trả",
                BorrowSlipDetailStatus.Overdue => "Quá hạn",
                BorrowSlipDetailStatus.Lost => "Bị mất",
                BorrowSlipDetailStatus.Damaged => "Hư hỏng",
                _ => "Không xác định"
            }
            : string.Empty;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
