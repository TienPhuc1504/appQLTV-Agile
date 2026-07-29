using Microsoft.Win32;

namespace LibraryManagement.App.Services;

public sealed class BookCoverPickerService : IBookCoverPickerService
{
    public string? PickCoverImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh bìa sách",
            Filter = "Tệp ảnh|*.jpg;*.jpeg;*.png;*.webp",
            CheckFileExists = true,
            Multiselect = false
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
