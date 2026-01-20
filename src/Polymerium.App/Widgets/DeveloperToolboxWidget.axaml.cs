using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Modals;
using Polymerium.App.Services;

namespace Polymerium.App.Widgets;

public partial class DeveloperToolboxWidget : WidgetBase
{
    public DeveloperToolboxWidget() => AvaloniaXamlLoader.Load(this);

    [RelayCommand]
    private void OpenJarInJarScanner()
    {
        var service = Context.Provider.GetRequiredService<OverlayService>();
        var modal = new JarInJarScannerWidgetModal();
        service.PopModal(modal);
    }
}
