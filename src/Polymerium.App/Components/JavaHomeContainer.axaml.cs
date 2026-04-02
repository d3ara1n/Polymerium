using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Services;
using Trident.Core.Utilities;

namespace Polymerium.App.Components;

public partial class JavaHomeContainer : UserControl
{
    private CancellationTokenSource? _captureHomeCts;
    private int _captureHomeVersion;

    public static readonly DirectProperty<JavaHomeContainer, string?> HomeProperty =
        AvaloniaProperty.RegisterDirect<JavaHomeContainer, string?>(
            nameof(Home),
            o => o.Home,
            (o, v) => o.Home = v
        );

    public static readonly DirectProperty<
        JavaHomeContainer,
        OverlayService?
    > OverlayServiceProperty = AvaloniaProperty.RegisterDirect<JavaHomeContainer, OverlayService?>(
        nameof(OverlayService),
        o => o.OverlayService,
        (o, v) => o.OverlayService = v
    );

    public static readonly DirectProperty<JavaHomeContainer, string?> VendorProperty =
        AvaloniaProperty.RegisterDirect<JavaHomeContainer, string?>(
            nameof(Vendor),
            o => o.Vendor,
            (o, v) => o.Vendor = v
        );

    public static readonly DirectProperty<JavaHomeContainer, string?> VersionProperty =
        AvaloniaProperty.RegisterDirect<JavaHomeContainer, string?>(
            nameof(Version),
            o => o.Version,
            (o, v) => o.Version = v
        );

    public static readonly DirectProperty<JavaHomeContainer, int?> MajorProperty =
        AvaloniaProperty.RegisterDirect<JavaHomeContainer, int?>(
            nameof(Major),
            o => o.Major,
            (o, v) => o.Major = v
        );

    public JavaHomeContainer() => InitializeComponent();

    public OverlayService? OverlayService
    {
        get;
        set => SetAndRaise(OverlayServiceProperty, ref field, value);
    }

    public string? Home
    {
        get;
        set => SetAndRaise(HomeProperty, ref field, value);
    }

    public string? Vendor
    {
        get;
        set => SetAndRaise(VendorProperty, ref field, value);
    }

    public string? Version
    {
        get;
        set => SetAndRaise(VersionProperty, ref field, value);
    }

    public int? Major
    {
        get;
        set => SetAndRaise(MajorProperty, ref field, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HomeProperty)
        {
            if (change.NewValue is not null)
            {
                StartCaptureHome(change.GetNewValue<string>());
            }
            else
            {
                CancelCaptureHome();
                Vendor = null;
                Version = null;
                Major = null;
            }
        }
    }

    private void StartCaptureHome(string home)
    {
        CancelCaptureHome();
        ClearJavaInfo();

        var version = unchecked(++_captureHomeVersion);
        var cts = new CancellationTokenSource();
        _captureHomeCts = cts;

        _ = CaptureHomeAsync(home, version, cts.Token);
    }

    private void CancelCaptureHome()
    {
        if (_captureHomeCts is { } cts)
        {
            _captureHomeCts = null;
            cts.Cancel();
            cts.Dispose();
        }
    }

    private async Task CaptureHomeAsync(
        string home,
        int version,
        CancellationToken cancellationToken
    )
    {
        var shouldCleanup = false;

        try
        {
            var info = await JavaHelper
                .ProbeHomeAsync(home, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!info.HasValue || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(
                () =>
                {
                    if (
                        cancellationToken.IsCancellationRequested
                        || version != _captureHomeVersion
                        || Home != home
                    )
                    {
                        return;
                    }

                    Vendor = info.Value.Vendor;
                    Version = info.Value.Version;
                    Major = info.Value.Major;
                },
                DispatcherPriority.Normal,
                cancellationToken
            );
        }
        catch (OperationCanceledException) { }
        finally
        {
            shouldCleanup = true;
        }

        if (!shouldCleanup)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_captureHomeCts != null && version == _captureHomeVersion && Home == home)
            {
                _captureHomeCts.Dispose();
                _captureHomeCts = null;
            }
        });
    }

    private void ClearJavaInfo()
    {
        Vendor = null;
        Version = null;
        Major = null;
    }

    #region Commands

    [RelayCommand]
    private void Remove() => Home = null;

    [RelayCommand]
    private async Task PickFileAsync()
    {
        if (OverlayService != null)
        {
            var path = await OverlayService.RequestFileAsync(
                Properties.Resources.JavaHomeContainer_RequestJavaPrompt,
                Properties.Resources.JavaHomeContainer_ReqeustJavaTitle
            );
            if (path != null && File.Exists(path))
            {
                var dir = Path.GetDirectoryName(Path.GetDirectoryName(path));
                if (dir != null)
                {
                    Home = dir;
                }
            }
        }
    }

    [RelayCommand]
    private void DetectHome()
    {
        // 未来也不会支持，单纯按钮放在这布局好看
    }

    #endregion
}
