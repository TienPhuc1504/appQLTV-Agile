using System.Globalization;
using System.Windows.Data;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.App.Converters;

public sealed class FineStatusConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is FineStatus status
            ? status switch
            {
                FineStatus.Unpaid => "Chưa thanh toán",
                FineStatus.PartiallyPaid => "Thanh toán một phần",
                FineStatus.Paid => "Đã thanh toán",
                FineStatus.Waived => "Đã miễn",
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
