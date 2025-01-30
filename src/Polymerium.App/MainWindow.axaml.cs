﻿using System;
using System.Linq;
using System.Windows.Input;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;

namespace Polymerium.App;

public partial class MainWindow : AppWindow
{
    private Action<Type, object?, IPageTransition?>? _navigate;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        ViewInstanceCommand = new RelayCommand<string>(ViewInstance);
    }

    public AvaloniaList<InstanceEntryModel> Entries { get; } = [];

    public ICommand ViewInstanceCommand { get; }

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
                Button { Tag: "CreateInstanceView" } => (typeof(CreateInstanceView), null),
                _ => (typeof(PageNotReachedView), null)
            };
            _navigate?.Invoke(target.Page, target.Parameter, null);
        }
    }

    private void ViewInstance(string? key)
    {
        if (key is not null)
            _navigate?.Invoke(typeof(InstanceView), key, null);
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

    internal void SubscribeProfileList(ProfileManager manager)
    {
        manager.ProfileAdded += OnProfileAdded;
        manager.ProfileUpdated += OnProfileUpdated;
        manager.ProfileRemoved += OnProfileRemoved;

        foreach (var (key, item) in manager.Profiles)
        {
            var model = new InstanceEntryModel(key, item.Name, item.Setup.Version, item.Setup.Loader,
                item.Setup.Source);
            Entries.Add(model);
        }
    }

    private void OnProfileAdded(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        if (Entries.Any(x => x.Basic.Key == e.Key)) return;
        var model = new InstanceEntryModel(e.Key, e.Value.Name, e.Value.Setup.Version, e.Value.Setup.Loader,
            e.Value.Setup.Source);
        Entries.Add(model);
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        var model = Entries.FirstOrDefault(x => x.Basic.Key == e.Key);
        if (model is not null)
        {
            // TODO
        }
    }

    private void OnProfileRemoved(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        var model = Entries.FirstOrDefault(x => x.Basic.Key == e.Key);
        if (model is not null) Entries.Remove(model);
    }

    #endregion

    #region State Service

    internal void SubscribeState(InstanceManager manager)
    {
        manager.InstanceInstalling += OnInstanceInstalling;
    }

    private void OnInstanceInstalling(object? sender, InstallTracker e)
    {
        var model = new InstanceEntryModel(e.Key, e.Key, "Unknown", null, null);
        model.State = InstanceEntryState.Installing;
        Entries.Add(model);
        // TODO: 通过 OnProfileUpdated 触发去更新 version loader source 以及 thumbnail
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

    #endregion
}