using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Huskui.Avalonia.Controls;

public class NotificationHost : ItemsControl
{
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        AvaloniaProperty.Register<NotificationHost, HorizontalAlignment>(nameof(HorizontalContentAlignment));

    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        AvaloniaProperty.Register<NotificationHost, VerticalAlignment>(nameof(VerticalContentAlignment));

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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AddHandler(NotificationItem.ClosedEvent, ItemClosedHandler);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        RemoveHandler(NotificationItem.ClosedEvent, ItemClosedHandler);
    }

    private void ItemClosedHandler(object? sender, RoutedEventArgs e)
    {
        if (e.Source is NotificationItem item)
            Items.Remove(item);
    }


    public void Pop(NotificationItem item)
    {
        Items.Add(item);
    }
}