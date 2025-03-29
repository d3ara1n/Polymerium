using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;

namespace Polymerium.App;

public partial class MainWindow : AppWindow
{
    public static readonly StyledProperty<bool> IsLeftPanelModeProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(IsLeftPanelMode));

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
    }

    public bool IsLeftPanelMode
    {
        get => GetValue(IsLeftPanelModeProperty);
        set => SetValue(IsLeftPanelModeProperty, value);
    }

    public static MainWindow Instance { get; private set; } = null!;

    public Frame.PageActivatorDelegate PageActivator
    {
        get => Root.PageActivator;
        set => Root.PageActivator = value;
    }

    #region Navigation Service

    internal void Navigate(Type page, object? parameter, IPageTransition transition) =>
        // NavigationService 会处理错误情况
        Root.Navigate(page, parameter, transition);

    internal bool CanGoBack() => Root.CanGoBack;

    internal void GoBack() => Root.GoBack();
    internal void ClearHistory() => Root.ClearHistory();

    #endregion

    #region Window State Management

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsLeftPanelModeProperty)
        {
            var mode = change.GetNewValue<bool>();
            if (mode)
            {
                if (Container.ColumnDefinitions is [var mainColumn, var sidebarColumn])
                {
                    Container.ColumnDefinitions = [sidebarColumn, mainColumn];

                    Main.SetValue(Grid.ColumnProperty, 1);
                    Sidebar.SetValue(Grid.ColumnProperty, 0);
                }
            }
            else
            {
                if (Container.ColumnDefinitions is [var sidebarColumn, var mainColumn])
                {
                    Container.ColumnDefinitions = [mainColumn, sidebarColumn];

                    Main.SetValue(Grid.ColumnProperty, 0);
                    Sidebar.SetValue(Grid.ColumnProperty, 1);
                }
            }
        }
    }

    #endregion
}