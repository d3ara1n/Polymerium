using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Polymerium.Avalonia.Controls;
using Polymerium.Avalonia.Models;
using Page = Huskui.Avalonia.Controls.Page;

namespace Polymerium.Avalonia.Pages;

public partial class SettingsPage : Page
{
    private readonly List<SettingsSectionModel> _sections = [];
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
        {
            NavList.SelectedIndex = 0;
        }

        AddHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged);
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e) =>
        RemoveHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged);

    private void BuildSections()
    {
        _sections.Clear();
        foreach (var child in ContentRoot.Children)
        {
            if (child is SettingsEntry entry && !string.IsNullOrEmpty(entry.Title))
            {
                _sections.Add(new() { Icon = entry.Icon, Target = entry });
            }
        }
    }

    private void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        var index = NavList.SelectedIndex;
        if (index >= 0 && index < _sections.Count)
        {
            AlignToTop(_sections[index].Target);
        }
    }

    // NOTE: 始终把节头停靠在视口顶部（对齐 macOS/Windows 设置页）；BringIntoView 是惰性的，
    //  只滚动到「恰好可见」，会与下方 top-anchored 的 spy 判定不一致导致高亮跳动。
    private void AlignToTop(Control target)
    {
        const double margin = 8d;
        var delta = TopOf(target) - margin;
        Scroller.Offset = new(Scroller.Offset.X, Scroller.Offset.Y + delta);
    }

    // NOTE: Top-anchored——节头到达锚线即激活，直至下一节头到达；视口触底时最后一段胜出，
    //  保证短尾节（头永远到不了顶）仍可选中。
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        const double anchor = 16d;
        var index = _sections.Count == 0 ? -1 : AtBottom() ? _sections.Count - 1 : IndexAtTop(anchor);

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
            {
                last = i;
            }
            else
            {
                break;
            }
        }

        return last;
    }

    private bool AtBottom() => Scroller.Viewport.Height > 0 && Scroller.Offset.Y >= Scroller.ScrollBarMaximum.Y - 0.5d;

    private double TopOf(Control target)
    {
        var transform = target.TransformToVisual(Scroller);
        return transform is null ? double.PositiveInfinity : transform.Value.Transform(new(0, 0)).Y;
    }
}
