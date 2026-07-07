using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Controls;

// Header/Entry 模板都拿到 item 本身（容器的 DataContext 永远是 item），模板内部从 Group 往里绑。
public class PackageListItemTemplateSelector : IDataTemplate
{
    public required DataTemplate HeaderTemplate { get; set; }

    public required DataTemplate EntryTemplate { get; set; }

    public bool SupportsRecycling => false;

    public Control? Build(object? param) =>
        param switch
        {
            PackageListItemBase.Header => HeaderTemplate.Build(param),
            PackageListItemBase.Entry => EntryTemplate.Build(param),
            _ => null
        };

    public bool Match(object? data) => data is PackageListItemBase;
}
