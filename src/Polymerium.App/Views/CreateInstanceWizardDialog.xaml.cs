// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class CreateInstanceWizardDialog : CustomDialog
{
    public CreateInstanceWizardDialog()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<CreateInstanceWizardViewModel>();
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }



    public IEnumerable<GameVersionModel> Versions
    {
        get { return (IEnumerable<GameVersionModel>)GetValue(VersionsProperty); }
        set { SetValue(VersionsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty VersionsProperty =
        DependencyProperty.Register(nameof(Versions), typeof(IEnumerable<GameVersionModel>), typeof(CreateInstanceWizardDialog), new PropertyMetadata(null));




    public bool IsOperable
    {
        get { return (bool)GetValue(IsOperableProperty); }
        set { SetValue(IsOperableProperty, value); }
    }

    public static readonly DependencyProperty IsOperableProperty =
        DependencyProperty.Register(nameof(IsOperable), typeof(bool), typeof(CreateInstanceWizardDialog), new PropertyMetadata(false));




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
        IsOperable = false;
        Task.Run(() => ViewModel.FillDataAsync(ViewModel_FillDataCompletedAsync), CancellationToken.None);
    }

    private Task ViewModel_FillDataCompletedAsync(IEnumerable<GameVersionModel> data)
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            IsOperable = true;
            Versions = data;
            CoreVersion.SelectedIndex = 0;
            VisualStateManager.GoToState(_root, "Default", false);
        });
        return Task.CompletedTask;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(_root, "Working", false);
        IsOperable = false;
        Task.Run(() => ViewModel.Commit(ViewModel_CommitCompletedAsync), CancellationToken.None);
    }

    private Task ViewModel_CommitCompletedAsync()
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            IsOperable = true;
            VisualStateManager.GoToState(_root, "Default", false);
            Dismiss();
        });
        return Task.CompletedTask;
    }
}
