using System;
using Avalonia;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Toasts;

public partial class ImageViewerToast : Toast
{
    public ImageViewerToast() => InitializeComponent();

    public static readonly StyledProperty<Uri> ImageSourceProperty =
        AvaloniaProperty.Register<ImageViewerToast, Uri>(nameof(ImageSource));

    public Uri ImageSource
    {
        get => GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }
}