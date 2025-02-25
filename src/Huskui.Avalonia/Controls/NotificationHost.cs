using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace Huskui.Avalonia.Controls;

public class NotificationHost : TemplatedControl
{
    public static readonly DirectProperty<NotificationHost, NotificationItems> ItemsProperty =
        AvaloniaProperty.RegisterDirect<NotificationHost, NotificationItems>(nameof(Items),
                                                                             o => o.Items,
                                                                             (o, v) => o.Items = v);

    public static readonly DirectProperty<NotificationHost, ITemplate<Panel?>> ItemsPanelProperty =
        AvaloniaProperty.RegisterDirect<NotificationHost, ITemplate<Panel?>>(nameof(ItemsPanel),
                                                                             o => o.ItemsPanel,
                                                                             (o, v) => o.ItemsPanel = v);

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        AvaloniaProperty.Register<NotificationHost, HorizontalAlignment>(nameof(HorizontalContentAlignment));

    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        AvaloniaProperty.Register<NotificationHost, VerticalAlignment>(nameof(VerticalContentAlignment));

    private NotificationItems _items = [];

    private ITemplate<Panel?> _itemsPanel = new FuncTemplate<Panel?>(() => new StackPanel());

    public NotificationItems Items
    {
        get => _items;
        set => SetAndRaise(ItemsProperty, ref _items, value);
    }

    public ITemplate<Panel?> ItemsPanel
    {
        get => _itemsPanel;
        set => SetAndRaise(ItemsPanelProperty, ref _itemsPanel, value);
    }

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }


    public void Pop(NotificationItem item)
    {
        // 脏方法实现的通知项生命周期+动画管理
        lock (this)
        {
            var toClean = Items.Where(x => !x.IsVisible);
            foreach (var clean in toClean)
                Items.Remove(clean);

            Items.Add(item);
        }
    }
}