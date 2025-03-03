using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":loading", ":finished", ":failed")]
public class Page : HeaderedContentControl
{
    public static readonly DirectProperty<Page, bool> CanGoBackProperty =
        Frame.CanGoBackProperty.AddOwner<Page>(o => o.CanGoBack,
                                               (o, v) => o.CanGoBack = v,
                                               defaultBindingMode: BindingMode.OneWay);

    public static readonly DirectProperty<Page, bool> IsHeaderVisibleProperty =
        AvaloniaProperty.RegisterDirect<Page, bool>(nameof(IsHeaderVisible),
                                                    o => o.IsHeaderVisible,
                                                    (o, v) => o.IsHeaderVisible = v);

    public static readonly DirectProperty<Page, bool> IsBackButtonVisibleProperty =
        AvaloniaProperty.RegisterDirect<Page, bool>(nameof(IsBackButtonVisible),
                                                    o => o.IsBackButtonVisible,
                                                    (o, v) => o.IsBackButtonVisible = v);

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _canGoBack;

    private bool _isBackButtonVisible = true;

    private bool _isHeaderVisible = true;

    public bool IsBackButtonVisible
    {
        get => _isBackButtonVisible;
        set => SetAndRaise(IsBackButtonVisibleProperty, ref _isBackButtonVisible, value);
    }

    public IPageModel? Model { get; set; }

    public bool CanGoBack
    {
        get => _canGoBack;
        set => SetAndRaise(CanGoBackProperty, ref _canGoBack, value);
    }

    public bool IsHeaderVisible
    {
        get => _isHeaderVisible;
        set => SetAndRaise(IsHeaderVisibleProperty, ref _isHeaderVisible, value);
    }

    protected override Type StyleKeyOverride => typeof(Page);

    // ReSharper disable once AsyncVoidMethod
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (!Design.IsDesignMode)
            if (Model is not null)
            {
                SetState(true);
                try
                {
                    await Task
                    .Run(async () =>
                    {
                        await Model.InitializeAsync(_cancellationTokenSource.Token);
                    });
                    SetState(false, true);
                }
                catch
                {
                    SetState(false, false, true);
                }
            }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (!Design.IsDesignMode)
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();

            if (Model is not null)
                Task.Run(async () => await Model.CleanupAsync(CancellationToken.None));
        }
    }

    private void SetState(bool loading = false, bool finished = false, bool failed = false)
    {
        PseudoClasses.Set(":loading", loading);
        PseudoClasses.Set(":finished", finished);
        PseudoClasses.Set(":failed", failed);
    }
}