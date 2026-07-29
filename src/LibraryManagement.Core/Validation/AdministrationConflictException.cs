namespace LibraryManagement.Core.Validation;

public sealed class AdministrationConflictException(string message)
    : InvalidOperationException(message);
