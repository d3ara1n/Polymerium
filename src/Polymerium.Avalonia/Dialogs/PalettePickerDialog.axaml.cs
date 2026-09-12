using Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Dialogs;

public partial class PalettePickerDialog : Dialog
{
    public static readonly DirectProperty<PalettePickerDialog, PalettePreviewItem?> SelectedProperty =
        AvaloniaProperty.RegisterDirect<PalettePickerDialog, PalettePreviewItem?>(nameof(Selected),
            o => o.Selected,
            (o, v) => o.Selected = v);

    public PalettePickerDialog() => InitializeComponent();

    public PalettePreviewItem? Selected
    {
        get;
        set
        {
            SetAndRaise(SelectedProperty, ref field, value);
            Result = value?.Palette;
        }
    }

    protected override bool ValidateResult(object? result) => result is Huskui.Avalonia.ColorPalette;
}
