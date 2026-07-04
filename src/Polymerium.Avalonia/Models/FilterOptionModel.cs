using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.Avalonia.Models;

public partial class FilterOptionModel : ObservableObject
{
    public FilterOptionModel(string value, string? label = null)
    {
        Value = value;
        Label = label ?? value;
    }

    public string Value { get; }

    public string Label { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
