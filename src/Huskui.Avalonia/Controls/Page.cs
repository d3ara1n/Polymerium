using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":loading", ":finished", ":failed")]
public class Page : HeaderedContentControl
{

    public static readonly DirectProperty<Page, bool> CanGoBackProperty =
        Frame.CanGoBackProperty.AddOwner<Page>(o => o.CanGoBack, (o, v) => o.CanGoBack = v,
            defaultBindingMode: BindingMode.OneWay);

    public static readonly DirectProperty<Page, bool> IsHeaderVisibleProperty =
        AvaloniaProperty.RegisterDirect<Page, bool>(nameof(IsHeaderVisible), o => o.IsHeaderVisible,
            (o, v) => o.IsHeaderVisible = v);

    private readonly CancellationTokenSource cancellationTokenSource = new();

    private bool _canGoBack;

    private bool _isHeaderVisible = true;
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

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (Model is not null)
        {
            SetState(true);
            Task.Run(
                async () => { await Model.InitializeAsync(Dispatcher.UIThread, cancellationTokenSource.Token); },
                cancellationTokenSource.Token).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                    Dispatcher.UIThread.Post(() => SetState(false, true));
                else if (t.IsCanceled)
                    Dispatcher.UIThread.Post(() => SetState(false, true));
                else if (t.IsFaulted) Dispatcher.UIThread.Post(() => SetState(false, false, true));
            });
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (!cancellationTokenSource.IsCancellationRequested) cancellationTokenSource.Cancel();
        if (Model is not null)
            Task.Run(async () => await Model.CleanupAsync(CancellationToken.None));
    }

    private void SetState(bool loading = false, bool finished = false, bool failed = false)
    {
        PseudoClasses.Set(":loading", loading);
        PseudoClasses.Set(":finished", finished);
        PseudoClasses.Set(":failed", failed);
    }
}