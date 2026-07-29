namespace LibraryManagement.App.Services;

public interface IDatabaseFilePickerService
{
    string? SelectBackupDestination();

    string? SelectRestoreSource();
}
