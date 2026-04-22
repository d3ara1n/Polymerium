using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Huskui.Avalonia;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.Mixins;
using Huskui.Avalonia.Mvvm.Models;
using Polymerium.App.Pages;

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
    }

    public static MainWindow Instance { get; private set; } = null!;

    internal void SetFrameActivator(IViewActivator activator) => FrameActivationMixin.Install(Root, activator);

    internal void SetTransparencyLevelHintByIndex(int index) =>
        TransparencyLevelHint = index switch
        {
            0 =>
            [
                WindowTransparencyLevel.Mica,
                WindowTransparencyLevel.AcrylicBlur,
                WindowTransparencyLevel.Blur,
                WindowTransparencyLevel.None,
            ],
            1 => [WindowTransparencyLevel.Mica, WindowTransparencyLevel.None],
            2 => [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None],
            3 => [WindowTransparencyLevel.Blur, WindowTransparencyLevel.None],
            _ => [WindowTransparencyLevel.None],
        };

    internal void SetThemeVariantByIndex(int index) =>
        Application.Current!.RequestedThemeVariant = index switch
        {
            0 => ThemeVariant.Default,
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

    internal void SetColorVariant(AccentColor accent)
    {
        if (Application.Current is App { Theme: { } theme })
        {
            theme.Accent = accent;
        }
    }

    internal void SetCornerStyle(CornerStyle corner)
    {
        if (Application.Current is App { Theme: { } theme })
        {
            theme.Corner = corner;
        }
    }

    private void DropContainer_OnDragOver(object? sender, DropContainer.DragOverEventArgs e)
    {
        if (e.Data.Contains(DataFormat.File))
        {
            e.IsValid = true;
        }
    }

    private void DropContainer_OnDrop(object? sender, DropContainer.DropEventArgs e)
    {
        if (e.Data.Contains(DataFormat.File))
        {
            var file = e.Data.TryGetFile();
            if (file != null)
            {
                var path = file.TryGetLocalPath();
                if (path != null && DataContext is MainWindowContext context)
                {
                    context.Navigate(typeof(NewInstancePage), path);
                }
            }
        }
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowContext context)
        {
            context.OnInitialize();
        }
    }

    private void Control_OnUnloded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowContext context)
        {
            context.OnDeinitialize();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsLeftPanelModeProperty)
        {
            var mode = change.GetNewValue<bool>();
            ApplySidebarPlacement(mode);
        }

        // IsTitleBarVisible 默认值是 false，此时连锁的连个属性也处于默认值，刚好
        if (change.Property == IsTitleBarVisibleProperty)
        {
            var visible = change.GetNewValue<bool>();
            if (visible)
            {
                ExtendClientAreaToDecorationsHint = true;
                // ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            }
            else
            {
                ExtendClientAreaToDecorationsHint = false;
                // ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.Default;
            }
        }
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
            case WindowState.FullScreen:
                WindowState = WindowState.Normal;
                break;
        }
    }

    private void TitleBarDragArea_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        BeginMoveDrag(e);
        e.Handled = true;
    }

    private void TitleBarDragArea_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (!CanResize)
        {
            return;
        }

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

    private void ApplySidebarPlacement(bool leftMode)
    {
        if ((Sidebar.GetValue(Grid.ColumnProperty) == 0) == leftMode)
        {
            return;
        }

        if (Container.ColumnDefinitions is not [var firstColumn, var secondColumn])
        {
            return;
        }

        Container.ColumnDefinitions = [secondColumn, firstColumn];

        if (leftMode)
        {
            Main.SetValue(Grid.ColumnProperty, 1);
            Sidebar.SetValue(Grid.ColumnProperty, 0);
            return;
        }

        Main.SetValue(Grid.ColumnProperty, 0);
        Sidebar.SetValue(Grid.ColumnProperty, 1);
    }

    #endregion
}
