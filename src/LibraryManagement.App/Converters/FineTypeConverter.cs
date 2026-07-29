using System.Globalization;
using System.Windows.Data;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.App.Converters;

public sealed class FineTypeConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is FineType fineType
            ? fineType switch
            {
                FineType.Overdue => "Quá hạn",
                FineType.Damaged => "Hư hỏng",
                FineType.Lost => "Mất sách",
                FineType.LostCard => "Mất thẻ",
                FineType.Other => "Khác",
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
