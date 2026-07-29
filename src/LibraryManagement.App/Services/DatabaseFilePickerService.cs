using Microsoft.Win32;

namespace LibraryManagement.App.Services;

public sealed class DatabaseFilePickerService : IDatabaseFilePickerService
{
    private const string DatabaseFilter =
        "SQLite database (*.db)|*.db|Tất cả file (*.*)|*.*";

    public string? SelectBackupDestination()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Chọn vị trí lưu bản sao database",
            Filter = DatabaseFilter,
            DefaultExt = ".db",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"LibraryManagement-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db"
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }

    public string? SelectRestoreSource()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn bản sao database cần phục hồi",
            Filter = DatabaseFilter,
            DefaultExt = ".db",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }
}
