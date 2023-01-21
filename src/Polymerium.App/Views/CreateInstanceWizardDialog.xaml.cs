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

namespace Polymerium.App.Views;

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




    public bool IsOpearable
    {
        get { return (bool)GetValue(IsOperableProperty); }
        set { SetValue(IsOperableProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsEnabled.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsOperableProperty =
        DependencyProperty.Register(nameof(IsOpearable), typeof(bool), typeof(CreateInstanceWizardDialog), new PropertyMetadata(false));




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
        IsOpearable = false;
        Task.Run(() => ViewModel.FillDataAsync(ViewModel_FillDataCompletedAsync), CancellationToken.None);
    }

    private Task ViewModel_FillDataCompletedAsync(IEnumerable<GameVersion> data)
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            IsOpearable = true;
            Versions = data;
            CoreVersion.SelectedIndex = 0;
            VisualStateManager.GoToState(_root, "Default", false);
        });
        return Task.CompletedTask;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(_root, "Working", false);
        IsOpearable = false;
        Task.Run(() => ViewModel.Commit(ViewModel_CommitCompletedAsync), CancellationToken.None);
    }

    private Task ViewModel_CommitCompletedAsync()
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            IsOpearable = true;
            VisualStateManager.GoToState(_root, "Default", false);
            Dismiss();
        });
        return Task.CompletedTask;
    }
}
