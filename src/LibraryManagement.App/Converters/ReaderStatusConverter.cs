using System.Globalization;
using System.Windows.Data;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.App.Converters;

public sealed class ReaderStatusConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is ReaderStatus status
            ? status switch
            {
                ReaderStatus.Active => "Đang hoạt động",
                ReaderStatus.Locked => "Đã khóa",
                ReaderStatus.Expired => "Hết hạn",
                ReaderStatus.Inactive => "Ngừng hoạt động",
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
