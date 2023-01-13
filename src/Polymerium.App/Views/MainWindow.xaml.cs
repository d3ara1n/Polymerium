// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Messages;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;
using WinUIEx;

namespace Polymerium.App.Views;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<MainViewModel>();

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            SetTitleBar(TitleBarDragArea);
            ExtendsContentIntoTitleBar = true;
        }
        else
        {
            (ColumnRight.Width, ColumnLeft.Width) = (ColumnLeft.Width, ColumnRight.Width);
        }

        if (Environment.OSVersion.Version.Major >= 10)
        {
            if (Environment.OSVersion.Version.Build >= 22000)
                Backdrop = new MicaSystemBackdrop();
            else
                Backdrop = new AcrylicSystemBackdrop();
        }
        else
        {
            FakeBackground.Visibility = Visibility.Visible;
        }
    }

    public MainViewModel ViewModel { get; }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var item = sender.SelectedItem as NavigationItemModel;
        RootFrame.Navigate(item.SourcePage, item.PageParameter, new SuppressNavigationTransitionInfo());
    }

    private void Main_Closed(object sender, WindowEventArgs args)
    {
        StrongReferenceMessenger.Default.Send(new ApplicationAliveChangedMessage(false));
    }
}
