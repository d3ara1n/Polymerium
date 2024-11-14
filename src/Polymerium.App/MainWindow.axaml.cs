using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Polymerium.App.Views;

namespace Polymerium.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Root.Navigate(typeof(WelcomeView));
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty)
        {
            (_oldCornerRadius, Sidebar.CornerRadius) = (Sidebar.CornerRadius, _oldCornerRadius);
            (_oldMargin, Sidebar.Margin) = (Sidebar.Margin, _oldMargin);
        }
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