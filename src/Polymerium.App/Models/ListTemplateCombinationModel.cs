using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using FluentIcons.Common;

namespace Polymerium.App.Models;

public class ListTemplateCombinationModel : AvaloniaObject
{
    public static readonly DirectProperty<ListTemplateCombinationModel, IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.RegisterDirect<ListTemplateCombinationModel, IDataTemplate?>(nameof(ItemTemplate),
            o => o.ItemTemplate,
            (o, v) => o.ItemTemplate = v);

    public IDataTemplate? ItemTemplate
    {
        get;
        set => SetAndRaise(ItemTemplateProperty, ref field, value);
    }

    public static readonly DirectProperty<ListTemplateCombinationModel, ItemsPanelTemplate?>
        ItemsPanelTemplateProperty =
            AvaloniaProperty
               .RegisterDirect<ListTemplateCombinationModel, ItemsPanelTemplate?>(nameof(ItemsPanelTemplate),
                    o => o.ItemsPanelTemplate,
                    (o, v) => o.ItemsPanelTemplate = v);

    public ItemsPanelTemplate? ItemsPanelTemplate
    {
        get;
        set => SetAndRaise(ItemsPanelTemplateProperty, ref field, value);
    }

    public static readonly DirectProperty<ListTemplateCombinationModel, Symbol> IconProperty =
        AvaloniaProperty.RegisterDirect<ListTemplateCombinationModel, Symbol>(nameof(Icon),
                                                                              o => o.Icon,
                                                                              (o, v) => o.Icon = v);

    public Symbol Icon
    {
        get;
        set => SetAndRaise(IconProperty, ref field, value);
    } = Symbol.Home;


    public static readonly DirectProperty<ListTemplateCombinationModel, ControlTheme?> ItemContainerThemeProperty =
        AvaloniaProperty.RegisterDirect<ListTemplateCombinationModel, ControlTheme?>(nameof(ItemContainerTheme),
            o => o.ItemContainerTheme,
            (o, v) => o.ItemContainerTheme = v);

    public ControlTheme? ItemContainerTheme
    {
        get;
        set => SetAndRaise(ItemContainerThemeProperty, ref field, value);
    }
}