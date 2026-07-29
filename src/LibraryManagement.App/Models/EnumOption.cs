namespace LibraryManagement.App.Models;

public sealed record EnumOption<T>(T Value, string DisplayName)
    where T : struct, Enum;
