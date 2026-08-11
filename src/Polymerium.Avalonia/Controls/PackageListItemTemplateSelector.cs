using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Controls;

// NOTE: 模板容器 DataContext 永远是 item 本身，Header/Entry 都从 item 往 Group 里绑。
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
