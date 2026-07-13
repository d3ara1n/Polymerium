using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Polymerium.Avalonia.Pages;

public partial class SettingsPage : Huskui.Avalonia.Controls.Page
{
    private readonly List<Models.SettingsSectionModel> _sections = [];
    private bool _syncing;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        BuildSections();
        NavList.ItemsSource = _sections;
        if (_sections.Count > 0)
            NavList.SelectedIndex = 0;

        AddHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged);
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e) =>
        RemoveHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged);

    private void BuildSections()
    {
        _sections.Clear();
        foreach (var child in ContentRoot.Children)
        {
            if (child is Controls.SettingsEntry entry && !string.IsNullOrEmpty(entry.Title))
                _sections.Add(new()
                {
                    Title = entry.Title,
                    Icon = entry.Icon,
                    Target = entry
                });
            else if (Controls.NavigationSectionProperties.GetTitle(child) is { Length: > 0 } title)
                _sections.Add(new()
                {
                    Title = title,
                    Icon = Controls.NavigationSectionProperties.GetIcon(child),
                    Target = child
                });
        }
    }

    private void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
            return;

        var index = NavList.SelectedIndex;
        if (index >= 0 && index < _sections.Count)
            AlignToTop(_sections[index].Target);
    }

    // Aligns the section header to the top of the viewport, matching macOS System
    // Settings / Windows Settings. Unlike BringIntoView — which is lazy and only scrolls
    // enough to reveal an already-visible target — this always parks the header at the
    // top, so the top-anchored spy below agrees with every click and the highlight never
    // snaps back to a different section.
    private void AlignToTop(Control target)
    {
        const double margin = 8d;
        var delta = TopOf(target) - margin;
        Scroller.Offset = new(Scroller.Offset.X, Scroller.Offset.Y + delta);
    }

    // Top-anchored: a section becomes active once its header reaches the anchor line and
    // stays active until the next section's header arrives there. When the viewport
    // bottoms out the last section wins, so short tails (whose headers can never reach
    // the top) are still selectable.
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        const double anchor = 16d;
        var index = _sections.Count == 0
                        ? -1
                        : AtBottom()
                            ? _sections.Count - 1
                            : IndexAtTop(anchor);

        if (index >= 0 && index != NavList.SelectedIndex)
        {
            _syncing = true;
            NavList.SelectedIndex = index;
            _syncing = false;
        }
    }

    private int IndexAtTop(double anchor)
    {
        var last = -1;
        for (var i = 0; i < _sections.Count; i++)
        {
            if (TopOf(_sections[i].Target) <= anchor)
                last = i;
            else
                break;
        }

        return last;
    }

    private bool AtBottom() =>
        Scroller.Viewport.Height > 0
        && Scroller.Offset.Y >= Scroller.ScrollBarMaximum.Y - 0.5d;

    private double TopOf(Control target)
    {
        var transform = target.TransformToVisual(Scroller);
        return transform is null
            ? double.PositiveInfinity
            : transform.Value.Transform(new(0, 0)).Y;
    }
}
