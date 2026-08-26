using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Polymerium.Avalonia.Controls;

public class GalleryCarousel : TemplatedControl
{
    public const string CLASS_Empty = ":empty";
    public const string CLASS_Single = ":single";

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<GalleryCarousel, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<GalleryCarousel, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<IDataTemplate?> ThumbnailTemplateProperty =
        AvaloniaProperty.Register<GalleryCarousel, IDataTemplate?>(nameof(ThumbnailTemplate));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<GalleryCarousel, int>(nameof(SelectedIndex));

    private INotifyCollectionChanged? _itemsSourceNotifier;

    public GalleryCarousel() => UpdateCollectionState(null);

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public IDataTemplate? ThumbnailTemplate
    {
        get => GetValue(ThumbnailTemplateProperty);
        set => SetValue(ThumbnailTemplateProperty, value);
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
            if (_itemsSourceNotifier is not null)
            {
                _itemsSourceNotifier.CollectionChanged -= OnItemsSourceCollectionChanged;
            }

            var items = change.GetNewValue<IEnumerable?>();
            _itemsSourceNotifier = items as INotifyCollectionChanged;
            if (_itemsSourceNotifier is not null)
            {
                _itemsSourceNotifier.CollectionChanged += OnItemsSourceCollectionChanged;
            }

            UpdateCollectionState(items);
        }
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        UpdateCollectionState(ItemsSource);

    private void UpdateCollectionState(IEnumerable? items)
    {
        var count = items?.Cast<object>().Take(2).Count() ?? 0;
        PseudoClasses.Set(CLASS_Empty, count == 0);
        PseudoClasses.Set(CLASS_Single, count == 1);
    }
}
