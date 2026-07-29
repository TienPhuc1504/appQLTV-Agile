namespace LibraryManagement.App.Models;

public sealed record EnumFilterOption<T>(T? Value, string DisplayName)
    where T : struct, Enum;
