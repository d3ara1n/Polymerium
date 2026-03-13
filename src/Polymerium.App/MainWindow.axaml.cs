using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Huskui.Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.App.Views;

namespace Polymerium.App;

public partial class MainWindow : AppWindow
{
    private static readonly TimeSpan SidebarPlacementAnimationDuration = TimeSpan.FromMilliseconds(240);

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
        ConfigureWindowChrome();
        _mainTransform.Transitions = CreateSidebarPlacementTransitions();
        _sidebarTransform.Transitions = CreateSidebarPlacementTransitions();
        Main.RenderTransform = _mainTransform;
        Sidebar.RenderTransform = _sidebarTransform;
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

    public bool IsMacOS => OperatingSystem.IsMacOS();

    private readonly TranslateTransform _mainTransform = new();
    private readonly TranslateTransform _sidebarTransform = new();
    private CancellationTokenSource? _sidebarPlacementAnimationTokenSource;
    private bool _isReadyToAnimateSidebarPlacement;

    public Frame.PageActivatorDelegate PageActivator
    {
        get => Root.PageActivator;
        set => Root.PageActivator = value;
    }

    private void ConfigureWindowChrome()
    {
        SystemDecorations = SystemDecorations.Full;

        if (OperatingSystem.IsMacOS())
        {
            ExtendClientAreaToDecorationsHint = false;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.Default;
            return;
        }

        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
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
                    context.Navigate(typeof(NewInstanceView), path);
                }
            }
        }
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _isReadyToAnimateSidebarPlacement = true;

        if (DataContext is MainWindowContext context)
        {
            context.OnInitialize();
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsLeftPanelModeProperty)
        {
            var mode = change.GetNewValue<bool>();
            _ = UpdateSidebarPlacementAsync(mode);
        }
    }

    private async Task UpdateSidebarPlacementAsync(bool leftMode)
    {
        _sidebarPlacementAnimationTokenSource?.Cancel();
        _sidebarPlacementAnimationTokenSource?.Dispose();

        using var cancellationTokenSource = new CancellationTokenSource();
        _sidebarPlacementAnimationTokenSource = cancellationTokenSource;

        try
        {
            if (!_isReadyToAnimateSidebarPlacement || Main.Bounds.Width <= 0 || Sidebar.Bounds.Width <= 0)
            {
                ResetSidebarTransforms();
                ApplySidebarPlacement(leftMode);
                return;
            }

            var sidebarIsOnLeft = IsSidebarOnLeft();
            if (sidebarIsOnLeft == leftMode)
            {
                ResetSidebarTransforms();
                return;
            }

            var mainWidth = Main.Bounds.Width;
            var sidebarWidth = Sidebar.Bounds.Width;
            var mainOffset = leftMode ? sidebarWidth : -sidebarWidth;
            var sidebarOffset = leftMode ? -mainWidth : mainWidth;

            SetSidebarPlacementTransitionsEnabled(true);
            _mainTransform.X = mainOffset;
            _sidebarTransform.X = sidebarOffset;

            await Task.Delay(SidebarPlacementAnimationDuration, cancellationTokenSource.Token);
            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            SetSidebarPlacementTransitionsEnabled(false);
            ApplySidebarPlacement(leftMode);
            ResetSidebarTransforms();
        }
        catch (OperationCanceledException)
        {
            // A newer sidebar placement request superseded the current animation.
            SetSidebarPlacementTransitionsEnabled(false);
            ResetSidebarTransforms();
        }
        finally
        {
            if (_sidebarPlacementAnimationTokenSource == cancellationTokenSource)
            {
                SetSidebarPlacementTransitionsEnabled(true);
                _sidebarPlacementAnimationTokenSource = null;
            }
        }
    }

    private void ApplySidebarPlacement(bool leftMode)
    {
        if (Container.ColumnDefinitions is not [var firstColumn, var secondColumn])
        {
            return;
        }

        if (IsSidebarOnLeft() == leftMode)
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

    private bool IsSidebarOnLeft() => Sidebar.GetValue(Grid.ColumnProperty) == 0;

    private void ResetSidebarTransforms()
    {
        _mainTransform.X = 0;
        _sidebarTransform.X = 0;
    }

    private void SetSidebarPlacementTransitionsEnabled(bool enabled)
    {
        _mainTransform.Transitions = enabled ? CreateSidebarPlacementTransitions() : null;
        _sidebarTransform.Transitions = enabled ? CreateSidebarPlacementTransitions() : null;
    }

    private static Transitions CreateSidebarPlacementTransitions() =>
        new()
        {
            new DoubleTransition
            {
                Property = TranslateTransform.XProperty,
                Duration = SidebarPlacementAnimationDuration,
                Easing = new CubicEaseOut()
            }
        };

    #endregion
}
