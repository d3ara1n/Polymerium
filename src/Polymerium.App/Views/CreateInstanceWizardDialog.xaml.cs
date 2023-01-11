// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Controls;
using Polymerium.App.ViewModels;

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
        Task.Run(() => ViewModel.FillDataAsync(ViewModel_FillDataCompleted), CancellationToken.None);
    }

    private async Task ViewModel_FillDataCompleted()
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            VisualStateManager.GoToState(_root, "Default", false));
    }
}
