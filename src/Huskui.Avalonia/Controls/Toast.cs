using Avalonia;
using Avalonia.Controls;

namespace Huskui.Avalonia.Controls;

public class Toast : ContentControl
{
    public static readonly DirectProperty<Toast, string> TitleProperty =
        AvaloniaProperty.RegisterDirect<Toast, string>(nameof(Title), o => o.Title, (o, v) => o.Title = v);

    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        set => SetAndRaise(TitleProperty, ref _title, value);
    }
}