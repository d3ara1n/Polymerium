using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Huskui.Avalonia.Controls;

namespace Polymerium.App;

public partial class MainWindow : AppWindow
{
    public static readonly StyledProperty<bool> IsLeftPanelModeProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(IsLeftPanelMode));

    public static readonly DirectProperty<MainWindow, bool> IsTitleBarVisibleProperty =
        AvaloniaProperty.RegisterDirect<MainWindow, bool>(nameof(IsTitleBarVisible),
                                                          o => o.IsTitleBarVisible,
                                                          (o, v) => o.IsTitleBarVisible = v);

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

    public bool IsTitleBarVisible
    {
        get;
        set => SetAndRaise(IsTitleBarVisibleProperty, ref field, value);
    } = true;


    public static MainWindow Instance { get; private set; } = null!;

    public Frame.PageActivatorDelegate PageActivator
    {
        get => Root.PageActivator;
        set => Root.PageActivator = value;
    }

    internal void SetTransparencyLevelHintByIndex(int index) =>
        TransparencyLevelHint = index switch
        {
            0 =>
            [
                WindowTransparencyLevel.Mica,
                WindowTransparencyLevel.AcrylicBlur,
                WindowTransparencyLevel.Blur,
                WindowTransparencyLevel.None
            ],
            1 => [WindowTransparencyLevel.Mica, WindowTransparencyLevel.None],
            2 => [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None],
            3 => [WindowTransparencyLevel.Blur, WindowTransparencyLevel.None],
            _ => [WindowTransparencyLevel.None]
        };

    internal void SetThemeVariantByIndex(int index) =>
        Application.Current!.RequestedThemeVariant = index switch
        {
            0 => ThemeVariant.Default,
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

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