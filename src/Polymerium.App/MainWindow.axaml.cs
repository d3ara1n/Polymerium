using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Transitions;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Polymerium.App.Views;
using Polymerium.Trident;
using Polymerium.Trident.Services;

namespace Polymerium.App;

public partial class MainWindow : AppWindow
{
    private Action<Type, object?, IPageTransition?>? _navigate;

    public AvaloniaList<DesktopItemModel> Profiles { get; } = new();

    public ICommand ViewInstanceCommand { get; }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        ViewInstanceCommand = new RelayCommand<DesktopItemModel>(ViewInstance);
    }

    private void PopDialog()
    {
        var pop = new Button
        {
            Content = "POP"
        };
        pop.Click += (_, __) => PopDialog();
        PopDialog(new Dialog
        {
            Title = $"DIALOG {Random.Shared.Next(1000, 9999)}",
            Message = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM",
            Background = Brushes.White,
            Content = new StackPanel
            {
                Spacing = 8d,
                Children =
                {
                    new TextBox(),
                    pop
                }
            }
        });
    }

    private void PopToast()
    {
        var pop = new Button
        {
            Content = "POP"
        };
        pop.Click += (_, __) => PopToast();
        PopToast(new Toast
        {
            Title = $"A VERY LONG TOAST TITLE {Random.Shared.Next(1000, 9999)}",
            Background = Brushes.White,
            Content = new StackPanel
            {
                Spacing = 8d,
                Children =
                {
                    new TextBlock { Text = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM" },
                    new TextBox(),
                    pop
                }
            }
        });
    }

    private void NavigationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: "42" })
        {
            PopToast();
        }
        else if (sender is Button { Tag: "0721" })
        {
            PopDialog();
        }
        else
        {
            (Type Page, object? Parameter) target = sender switch
            {
                Button { Tag: "ExhibitionView" } => (typeof(ExhibitionView), null),
                Button { Tag: "UnknownView" } => (typeof(UnknownView), Random.Shared.Next(1000, 9999)),
                _ => (typeof(NotFoundView), null)
            };
            Navigate(target.Page, target.Parameter, null);
        }
    }

    private void ViewInstance(DesktopItemModel? model)
    {
        if (model is not null)
            Navigate(typeof(InstanceView), model.Key, null);
    }

    #region Navigation Service

    internal void Navigate(Type page, object? parameter, IPageTransition? transition)
    {
        Root.Navigate(page, parameter, transition ?? (page.IsAssignableTo(typeof(ScopedPage))
            ? new PageCoverOverTransition(TimeSpan.FromMilliseconds(197), DirectionFrom.Right)
            : new PopUpTransition(TimeSpan.FromMilliseconds(197))));
    }

    internal void BindNavigation(Action<Type, object?, IPageTransition?> navigate,
        Frame.PageActivatorDelegate activator)
    {
        _navigate = navigate;
        Root.PageActivator = activator;
    }

    #endregion

    #region Profile Service

    private string? SearchIcon(string key)
    {
        var home = PathDef.Default.DirectoryOfHome(key);
        string[] candidates = ["icon.png", "icon.jpeg", "icon.jpg", "icon.webp", "icon.bmp"];
        foreach (var candidate in candidates)
        {
            var path = Path.Combine(home, candidate);
            if (File.Exists(path)) return path;
        }

        return null;
    }

    internal void SubscribeProfileList(ProfileService profile)
    {
        profile.ProfileAdded += OnProfileAdded;
        profile.ProfileUpdated += OnProfileUpdated;
        profile.ProfileRemoved += OnProfileRemoved;

        foreach (var (key, item) in profile.Profiles)
        {
            var model = DesktopItemModel.From(key, item, SearchIcon(key));
            Profiles.Add(model);
        }
    }

    private void OnProfileAdded(object? sender, ProfileService.ProfileChangedEventArgs e)
    {
        var model = DesktopItemModel.From(e.Key, e.Value, SearchIcon(e.Key));
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