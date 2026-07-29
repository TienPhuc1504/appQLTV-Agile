using CommunityToolkit.Mvvm.ComponentModel;

namespace LibraryManagement.App.Models;

public sealed partial class SelectableLookupItem(int id, string name)
    : ObservableObject
{
    public int Id { get; } = id;

    public string Name { get; } = name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
