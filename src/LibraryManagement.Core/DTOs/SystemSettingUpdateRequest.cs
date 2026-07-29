namespace LibraryManagement.Core.DTOs;

public sealed record SystemSettingUpdateRequest(
    string Key,
    string Value);
