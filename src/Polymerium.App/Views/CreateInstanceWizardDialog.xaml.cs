// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions.DownloadSources.Models;
using Polymerium.App.Controls;
using Polymerium.App.ViewModels;
using static System.Runtime.InteropServices.JavaScript.JSType;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CreateInstanceWizardDialog : CustomDialog
{
    public CreateInstanceWizardDialog()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<CreateInstanceWizardViewModel>();
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }



    public IEnumerable<GameVersion> Versions
    {
        get { return (IEnumerable<GameVersion>)GetValue(VersionsProperty); }
        set { SetValue(VersionsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty VersionsProperty =
        DependencyProperty.Register(nameof(Versions), typeof(IEnumerable<GameVersion>), typeof(CreateInstanceWizardDialog), new PropertyMetadata(null));




    public bool IsOpeable
    {
        get { return (bool)GetValue(IsOpeableProperty); }
        set { SetValue(IsOpeableProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsEnabled.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsOpeableProperty =
        DependencyProperty.Register(nameof(IsOpeable), typeof(bool), typeof(CreateInstanceWizardDialog), new PropertyMetadata(false));




    public CreateInstanceWizardViewModel ViewModel { get; }

    private readonly DispatcherQueue _dispatcher;

    private ContentControl _root;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = FindName("Root") as ContentControl;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }

    private void CreateInstanceWizardDialog_OnLoaded(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(_root, "Loading", false);
        IsOpeable = false;
        Task.Run(() => ViewModel.FillDataAsync(ViewModel_FillDataCompletedAsync), CancellationToken.None);
    }

    private Task ViewModel_FillDataCompletedAsync(IEnumerable<GameVersion> data)
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            IsOpeable = true;
            Versions = data;
            CoreVersion.SelectedIndex = 0;
            VisualStateManager.GoToState(_root, "Default", false);
        });
        return Task.CompletedTask;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(_root, "Working", false);
        IsOpeable = false;
        Task.Run(() => ViewModel.Commit(ViewModel_CommitCompletedAsync), CancellationToken.None);
    }

    private Task ViewModel_CommitCompletedAsync()
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            IsOpeable = true;
            VisualStateManager.GoToState(_root, "Default", false);
            Dismiss();
        });
        return Task.CompletedTask;
    }
}
