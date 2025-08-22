using Avalonia;
using Avalonia.Controls;

namespace Polymerium.App.Controls;

public class StorageInstanceButton : Button
{
    public static readonly DirectProperty<StorageInstanceButton, ulong> TotalSizeProperty =
        AvaloniaProperty.RegisterDirect<StorageInstanceButton, ulong>(nameof(TotalSize),
            o => o.TotalSize,
            (o, v) => o.TotalSize = v);

    public ulong TotalSize
    {
        get;
        set => SetAndRaise(TotalSizeProperty, ref field, value);
    }
}