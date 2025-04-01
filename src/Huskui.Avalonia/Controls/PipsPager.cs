using Avalonia;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

public class PipsPager : TemplatedControl
{
    public static readonly StyledProperty<int> ItemCountProperty =
        AvaloniaProperty.Register<PipsPager, int>(nameof(ItemCount));


    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<PipsPager, int>(nameof(SelectedIndex));

    public int ItemCount
    {
        get => GetValue(ItemCountProperty);
        set => SetValue(ItemCountProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }
}