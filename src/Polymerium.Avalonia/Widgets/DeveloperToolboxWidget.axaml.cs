using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia.Widgets;

public partial class DeveloperToolboxWidget : WidgetBase
{
    public DeveloperToolboxWidget() => AvaloniaXamlLoader.Load(this);

    [RelayCommand]
    private void OpenJarInJarScanner()
    {
        var service = Context.Provider.GetRequiredService<OverlayService>();
        service.PopModal<JarInJarScannerWidgetModal>(Context.Key);
    }
}
