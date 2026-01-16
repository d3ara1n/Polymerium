using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Polymerium.App.Widgets;

public partial class DeveloperToolboxWidget : WidgetBase
{
    public DeveloperToolboxWidget() => AvaloniaXamlLoader.Load(this);
}
