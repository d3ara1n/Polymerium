using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using IconPacks.Avalonia.Lucide;

namespace Polymerium.App.Models
{
    public class ListTemplateCombinationModel : AvaloniaObject
    {
        public static readonly DirectProperty<ListTemplateCombinationModel, IDataTemplate?> ItemTemplateProperty =
            AvaloniaProperty.RegisterDirect<ListTemplateCombinationModel, IDataTemplate?>(nameof(ItemTemplate),
                o => o.ItemTemplate,
                (o, v) => o.ItemTemplate = v);

        public static readonly DirectProperty<ListTemplateCombinationModel, ItemsPanelTemplate?>
            ItemsPanelTemplateProperty =
                AvaloniaProperty
                   .RegisterDirect<ListTemplateCombinationModel, ItemsPanelTemplate?>(nameof(ItemsPanelTemplate),
                        o => o.ItemsPanelTemplate,
                        (o, v) => o.ItemsPanelTemplate = v);

        public static readonly DirectProperty<ListTemplateCombinationModel, PackIconLucideKind> IconProperty =
            AvaloniaProperty.RegisterDirect<ListTemplateCombinationModel, PackIconLucideKind>(nameof(Icon),
                o => o.Icon,
                (o, v) => o.Icon = v);


        public static readonly DirectProperty<ListTemplateCombinationModel, ControlTheme?> ItemContainerThemeProperty =
            AvaloniaProperty.RegisterDirect<ListTemplateCombinationModel, ControlTheme?>(nameof(ItemContainerTheme),
                o => o.ItemContainerTheme,
                (o, v) => o.ItemContainerTheme = v);

        public IDataTemplate? ItemTemplate
        {
            get;
            set => SetAndRaise(ItemTemplateProperty, ref field, value);
        }

        public ItemsPanelTemplate? ItemsPanelTemplate
        {
            get;
            set => SetAndRaise(ItemsPanelTemplateProperty, ref field, value);
        }

        public PackIconLucideKind Icon
        {
            get;
            set => SetAndRaise(IconProperty, ref field, value);
        } = PackIconLucideKind.House;

        public ControlTheme? ItemContainerTheme
        {
            get;
            set => SetAndRaise(ItemContainerThemeProperty, ref field, value);
        }
    }
}
