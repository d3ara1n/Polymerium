using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Polymerium.Avalonia.Controls;

public class GalleryCarousel : TemplatedControl
{
    public const string CLASS_Empty = ":empty";
    public const string CLASS_Single = ":single";

    public static readonly StyledProperty<IReadOnlyList<Uri>?> ItemsSourceProperty =
        AvaloniaProperty.Register<GalleryCarousel, IReadOnlyList<Uri>?>(nameof(ItemsSource));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<GalleryCarousel, int>(nameof(SelectedIndex));

    public GalleryCarousel() => UpdateCollectionState(null);

    public IReadOnlyList<Uri>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty)
        {
            UpdateCollectionState(change.GetNewValue<IReadOnlyList<Uri>?>());
        }
    }

    private void UpdateCollectionState(IReadOnlyList<Uri>? items)
    {
        PseudoClasses.Set(CLASS_Empty, items is null || items.Count == 0);
        PseudoClasses.Set(CLASS_Single, items?.Count == 1);
    }
}
