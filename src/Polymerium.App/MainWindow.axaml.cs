using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;
using Polymerium.App.Views;

namespace Polymerium.App;

public partial class MainWindow : Window
{
    private Action<Type, object?, IPageTransition?>? _navigate;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Navigate(typeof(ExhibitionView), Random.Shared.Next(1000, 9999),
            new PageSlide(TimeSpan.FromMilliseconds(150)));
    }

    internal void Navigate(Type page, object? parameter, IPageTransition transition)
    {
        Root.Navigate(page, parameter, transition);
    }

    internal void BindNavigation(Action<Type, object?, IPageTransition?> navigate,
        Frame.PageActivatorDelegate activator)
    {
        _navigate = navigate;
        Root.PageActivator = activator;
    }

    #region Window State Management

    private CornerRadius _oldCornerRadius = new(0);
    private Thickness _oldMargin = new(0);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty && Root.Content is Page page)
        {
            (page.CornerRadius, _oldCornerRadius) = (_oldCornerRadius, page.CornerRadius);
            (page.Margin, _oldMargin) = (_oldMargin, page.Margin);
        }
    }

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

    private void Window_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void Window_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Pointer.Captured is null)
            switch (WindowState)
            {
                case WindowState.Maximized:
                    WindowState = WindowState.Normal;
                    break;
                case WindowState.Normal:
                    WindowState = WindowState.Maximized;
                    break;
            }
    }

    #endregion
}