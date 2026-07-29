using System.Globalization;
using System.Windows.Data;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.App.Converters;

public sealed class BookCopyStatusConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is BookCopyStatus status
            ? status switch
            {
                BookCopyStatus.Available => "Có sẵn",
                BookCopyStatus.Borrowed => "Đang mượn",
                BookCopyStatus.Damaged => "Hư hỏng",
                BookCopyStatus.Lost => "Bị mất",
                BookCopyStatus.Maintenance => "Bảo trì",
                BookCopyStatus.Inactive => "Ngừng sử dụng",
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
