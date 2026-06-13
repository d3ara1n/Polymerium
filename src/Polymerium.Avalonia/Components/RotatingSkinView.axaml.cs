using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Components;

/// <summary>
///     Cross-fades through a stack of directional skin renders (front → right → back → left)
///     to give the illusion of a slowly rotating character. Auto-advances, pauses on hover,
///     and exposes manual prev/next navigation.
/// </summary>
public partial class RotatingSkinView : UserControl
{
    public static readonly StyledProperty<IReadOnlyList<Uri>?> SourcesProperty =
        AvaloniaProperty.Register<RotatingSkinView, IReadOnlyList<Uri>?>(nameof(Sources));

    public static readonly StyledProperty<Uri?> FallbackProperty =
        AvaloniaProperty.Register<RotatingSkinView, Uri?>(nameof(Fallback));

    private readonly DispatcherTimer _timer;
    private int _index;
    private bool _isLoaded;

    public RotatingSkinView()
    {
        InitializeComponent();
        // Wire the frames collection straight onto the two ItemsControls in code-behind. This
        // avoids fragile element-name bindings (#Root.X) that depend on UserControl name-scope
        // resolution and are easy to get silently wrong.
        FramesList.ItemsSource = Frames;
        Indicators.ItemsSource = Frames;

        _timer = new(
                     TimeSpan.FromMilliseconds(1200),
                     DispatcherPriority.Normal,
                     (_, _) => Advance(1)
                    );
        Loaded += (_, _) =>
        {
            _isLoaded = true;
            StartTimerIfEligible();
        };
        Unloaded += (_, _) =>
        {
            _isLoaded = false;
            _timer.Stop();
        };
    }

    /// <summary>
    ///     The ordered list of render URLs to cycle through (front → right → back → left).
    /// </summary>
    public IReadOnlyList<Uri>? Sources
    {
        get => GetValue(SourcesProperty);
        set => SetValue(SourcesProperty, value);
    }

    /// <summary>
    ///     Optional static image shown when <see cref="Sources" /> is empty (e.g. offline).
    /// </summary>
    public Uri? Fallback
    {
        get => GetValue(FallbackProperty);
        set => SetValue(FallbackProperty, value);
    }

    /// <summary>
    ///     Internal frames driving the cross-fade; populated from <see cref="Sources" />.
    /// </summary>
    public ObservableCollection<SkinFrameModel> Frames { get; } = [];

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourcesProperty)
            RebuildFrames();
    }

    private void RebuildFrames()
    {
        _index = 0;
        Frames.Clear();
        if (Sources is { Count: > 0 })
        {
            foreach (var url in Sources)
                Frames.Add(new SkinFrameModel(url));
        }
        else if (Fallback is not null)
        {
            // No directional renders available (e.g. offline); fall back to a single static image.
            Frames.Add(new SkinFrameModel(Fallback));
        }

        if (Frames.Count > 0)
            Frames[0].IsActive = true;

        var multi = Frames.Count > 1;
        PreviousButton.IsVisible = multi;
        NextButton.IsVisible = multi;
        StartTimerIfEligible();
    }

    private void StartTimerIfEligible()
    {
        _timer.Stop();
        if (_isLoaded && Frames.Count > 1)
            _timer.Start();
    }

    private void Advance(int delta)
    {
        if (Frames.Count <= 1)
            return;
        Frames[_index].IsActive = false;
        _index = (_index + delta + Frames.Count) % Frames.Count;
        Frames[_index].IsActive = true;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e) => _timer.Stop();

    private void OnPointerExited(object? sender, PointerEventArgs e) => StartTimerIfEligible();

    private void OnPreviousClicked(object? sender, RoutedEventArgs e)
    {
        Advance(-1);
        StartTimerIfEligible();
    }

    private void OnNextClicked(object? sender, RoutedEventArgs e)
    {
        Advance(1);
        StartTimerIfEligible();
    }
}
