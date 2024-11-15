using Avalonia;
using Avalonia.Controls.Primitives;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Controls;

public class PageHeaderBar: TemplatedControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<Page, string>(nameof(Page), string.Empty);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}