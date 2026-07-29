using System.Globalization;
using System.Windows.Data;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.App.Converters;

public sealed class PhysicalConditionConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is PhysicalCondition condition
            ? condition switch
            {
                PhysicalCondition.New => "Mới",
                PhysicalCondition.Good => "Tốt",
                PhysicalCondition.Worn => "Cũ",
                PhysicalCondition.Damaged => "Hư hỏng",
                PhysicalCondition.Lost => "Bị mất",
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
