using System.Globalization;
using System.Windows.Data;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.App.Converters;

public sealed class ReaderTypeConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is ReaderType readerType
            ? readerType switch
            {
                ReaderType.Student => "Sinh viên",
                ReaderType.Lecturer => "Giảng viên",
                ReaderType.Adult => "Người lớn",
                ReaderType.Child => "Trẻ em",
                ReaderType.Other => "Khác",
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
