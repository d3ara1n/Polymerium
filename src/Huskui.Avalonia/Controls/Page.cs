using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":loading", ":finished", ":failed")]
public class Page : ContentControl
{
    public const string PART_ContentPresenter = nameof(PART_ContentPresenter);

    private readonly CancellationTokenSource cancellationTokenSource = new();
    public IPageModel? Model { get; set; }

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

        cancellationTokenSource.Cancel();
    }

    private void SetState(bool loading = false, bool finished = false, bool failed = false)
    {
        Debug.WriteLine($"(:loading, :finished, :failed)=({loading}, {finished}, {failed})");
        PseudoClasses.Set(":loading", loading);
        PseudoClasses.Set(":finished", finished);
        PseudoClasses.Set(":failed", failed);
    }
}