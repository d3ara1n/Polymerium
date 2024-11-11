﻿using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DynamicData.Binding;

namespace Polymerium.App;

[PseudoClasses(":maximized", ":minimized", "normal")]
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WindowStateProperty.Changed.AddClassHandler<MainWindow>(OnWindowStateChanged);
    }

    #region Window State Management

    private CornerRadius _oldCornerRadius = new(0);
    private Thickness _oldMargin = new(0);

    private void ToggleMaximize()
    {
        switch (WindowState)
        {
            case WindowState.Normal:
                WindowState = WindowState.Maximized;
                break;
            case WindowState.Maximized:
                WindowState = WindowState.Normal;
                break;
        }
    }

    private void OnWindowStateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        (_oldCornerRadius, Sidebar.CornerRadius) = (Sidebar.CornerRadius, _oldCornerRadius);
        (_oldMargin, Sidebar.Margin) = (Sidebar.Margin, _oldMargin);
    }

    private void Sidebar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
        e.Handled = true;
    }

    private void Sidebar_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (!Equals(e.Source, Sidebar)) return;
        ToggleMaximize();
        e.Handled = true;
    }

    private void MinimizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
        e.Handled = true;
    }

    private void ToggleMaximizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ToggleMaximize();
        e.Handled = true;
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }

    #endregion
}