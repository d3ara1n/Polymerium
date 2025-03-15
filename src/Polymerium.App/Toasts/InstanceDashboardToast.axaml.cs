using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Huskui.Avalonia.Controls;
using ObservableCollections;
using Polymerium.Trident.Engines.Launching;

namespace Polymerium.App.Toasts;

public partial class InstanceDashboardToast : Toast
{
    public static readonly DirectProperty<InstanceDashboardToast, NotifyCollectionChangedSynchronizedViewList<Scrap>?>
        BindableProperty =
            AvaloniaProperty
               .RegisterDirect<InstanceDashboardToast, NotifyCollectionChangedSynchronizedViewList<Scrap>
                    ?>(nameof(Bindable), o => o.Bindable, (o, v) => o.Bindable = v);

    public static readonly DirectProperty<InstanceDashboardToast, string> FilterTextProperty =
        AvaloniaProperty.RegisterDirect<InstanceDashboardToast, string>(nameof(FilterText),
                                                                        o => o.FilterText,
                                                                        (o, v) => o.FilterText = v);

    public static readonly DirectProperty<InstanceDashboardToast, bool> IsAutoScrollProperty =
        AvaloniaProperty.RegisterDirect<InstanceDashboardToast, bool>(nameof(IsAutoScroll),
                                                                      o => o.IsAutoScroll,
                                                                      (o, v) => o.IsAutoScroll = v,
                                                                      true);

    private int _debounce;


    private ISynchronizedView<Scrap, Scrap>? _view;

    public InstanceDashboardToast()
    {
        InitializeComponent();
        AddHandler(ScrollViewer.ScrollChangedEvent, ViewerOnScrollChanged);
    }

    public NotifyCollectionChangedSynchronizedViewList<Scrap>? Bindable
    {
        get => field;
        set => SetAndRaise(BindableProperty, ref field, value);
    }

    public bool IsAutoScroll
    {
        get => field;
        set => SetAndRaise(IsAutoScrollProperty, ref field, value);
    } = true;


    public string FilterText
    {
        get => field;
        set => SetAndRaise(FilterTextProperty, ref field, value);
    } = string.Empty;

    private void ViewerOnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (e.OffsetDelta.Y < 0)
        {
            _debounce++;
            if (_debounce > 3)
            {
                IsAutoScroll = false;
                _debounce = 0;
            }
        }
        else
        {
            _debounce = 0;
        }
    }

    public void SetItems(ObservableFixedSizeRingBuffer<Scrap> source)
    {
        _view = source.CreateView(x => x);
        Bindable = _view.ToNotifyCollectionChanged();
        Bindable.CollectionChanged += BindableOnCollectionChanged;
    }

    private void BindableOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
            Dispatcher.UIThread.Post(() =>
            {
                if (IsAutoScroll)
                    Viewer.ScrollToEnd();
            });
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (Bindable != null)
            Bindable.CollectionChanged -= BindableOnCollectionChanged;
        Bindable?.Dispose();
        _view?.Dispose();
        RemoveHandler(ScrollViewer.ScrollChangedEvent, ViewerOnScrollChanged);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FilterTextProperty)
        {
            var filter = change.GetNewValue<string>();
            _view?.AttachFilter(x => string.IsNullOrEmpty(filter) || x.Message.Contains(filter));
        }
    }
}