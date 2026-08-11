using Avalonia;
using Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Controls;

// NOTE: 按 Selection 派生类型（Default/System/File）渲染字体预览；chip 与 FontPickerDialog 顶部预览共用。
public partial class FontSelectionPresenter : UserControl
{
    public static readonly StyledProperty<FontModelBase?> SelectionProperty =
        AvaloniaProperty.Register<FontSelectionPresenter, FontModelBase?>(nameof(Selection));

    public FontSelectionPresenter() => InitializeComponent();

    public FontModelBase? Selection
    {
        get => GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }
}
