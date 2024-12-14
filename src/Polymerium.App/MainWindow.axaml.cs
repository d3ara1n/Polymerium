using System;
using System.Linq;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using Polymerium.App.Views;
using Polymerium.Trident.Services;

namespace Polymerium.App;

public partial class MainWindow : AppWindow
{
    private Action<Type, object?, IPageTransition?>? _navigate;

    public AvaloniaList<DesktopItemModel> Profiles { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }
    
    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Navigate(sender switch
            {
                Button { Tag: "ExhibitionView" } => typeof(ExhibitionView),
                Button { Tag: "UnknownView" } => typeof(UnknownView),
                _ => typeof(NotFoundView)
            }, Random.Shared.Next(1000, 9999),
            new PageSlide(TimeSpan.FromMilliseconds(150)));
    }

    #region Navigation Service

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

    #endregion

    #region Profile Service

    internal void SubscribeProfileList(ProfileService profile)
    {
        profile.ProfileAdded += OnProfileAdded;
        profile.ProfileUpdated += OnProfileUpdated;
        profile.ProfileRemoved += OnProfileRemoved;

        foreach (var (key, item) in profile.Profiles)
        {
            var model = DesktopItemModel.From(key, item);
            Profiles.Add(model);
        }
    }

    private void OnProfileAdded(object? sender, ProfileService.ProfileChangedEventArgs e)
    {
        var model = DesktopItemModel.From(e.Key, e.Value);
        Profiles.Add(model);
    }

    private void OnProfileUpdated(object? sender, ProfileService.ProfileChangedEventArgs e)
    {
        var model = Profiles.FirstOrDefault(x => x.Key == e.Key);
        if (model is not null) model.Update(e.Value);
    }

    private void OnProfileRemoved(object? sender, ProfileService.ProfileChangedEventArgs e)
    {
        var model = Profiles.FirstOrDefault(x => x.Key == e.Key);
        if (model is not null) Profiles.Remove(model);
    }

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