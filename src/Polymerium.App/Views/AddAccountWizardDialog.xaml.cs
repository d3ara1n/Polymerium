// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Polymerium.App.Controls;
using Polymerium.App.Services;
using Polymerium.App.ViewModels;
using Polymerium.App.Views.AddAccountWizards;

namespace Polymerium.App.Views;

public delegate void AddAccountWizardStateHandler(
    (Type, object?)? nextPage,
    bool isLast = false,
    Func<bool>? finishAction = null
);

public sealed partial class AddAccountWizardDialog : CustomDialog
{
    private readonly AddAccountWizardStateHandler handler;
    private Func<bool>? finish;
    private (Type, object?)? next;

    public AddAccountWizardDialog(IOverlayService overlayService)
    {
        ViewModel = App.Current.Provider.GetRequiredService<AddAccountWizardViewModel>();
        InitializeComponent();
        OverlayService = overlayService;
        handler = SetState;
        Root.Navigate(
            typeof(AccountSelectionView),
            (handler, ViewModel.Source.Token, (object?)null)
        );
    }

    public AddAccountWizardViewModel ViewModel { get; }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Source.Cancel();
        Dismiss();
    }

    private void SetState((Type, object?)? nextPage, bool isLast, Func<bool>? finishAction)
    {
        if (nextPage != null)
        {
            NextButton.Visibility = Visibility.Visible;
            FinishButton.Visibility = Visibility.Collapsed;
            NextButton.IsEnabled = true;
        }
        else
        {
            if (isLast)
            {
                NextButton.Visibility = Visibility.Collapsed;
                FinishButton.Visibility = Visibility.Visible;
            }
            else
            {
                NextButton.Visibility = Visibility.Visible;
                FinishButton.Visibility = Visibility.Collapsed;
                NextButton.IsEnabled = false;
            }
        }

        finish = finishAction;
        next = nextPage;
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        Root.Navigate(next!.Value.Item1, (handler, ViewModel.Source.Token, next!.Value.Item2));
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        Root.GoBack();
    }

    private void FinishButton_Click(object sender, RoutedEventArgs e)
    {
        if (finish?.Invoke() ?? true)
            Dismiss();
    }
}
