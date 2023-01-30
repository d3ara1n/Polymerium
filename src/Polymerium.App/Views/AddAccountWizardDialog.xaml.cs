// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Polymerium.App.Controls;
using Polymerium.App.Services;
using Polymerium.App.ViewModels;
using Polymerium.App.Views.AddAccountWizard;

namespace Polymerium.App.Views;

public delegate void AddAccountWizardStateHandler(Type nextPage, bool isLast = false, Func<bool> finishAction = null);

public sealed partial class AddAccountWizardDialog : CustomDialog
{
    private readonly AddAccountWizardStateHandler handler;
    private Func<bool> finish;
    private Type next;

    public AddAccountWizardDialog(IOverlayService overlayService)
    {
        InitializeComponent();
        OverlayService = overlayService;
        ViewModel = App.Current.Provider.GetRequiredService<AddAccountWizardViewModel>();
        handler = SetState;
        Root.Navigate(typeof(SelectionView), (handler, ViewModel.Source.Token));
    }

    public AddAccountWizardViewModel ViewModel { get; }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Source.Cancel();
        Dismiss();
    }

    private void SetState(Type nextPage, bool isLast, Func<bool> finishAction)
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
        Root.Navigate(next, (handler, ViewModel.Source.Token));
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        Root.GoBack();
    }

    private void FinishButton_Click(object sender, RoutedEventArgs e)
    {
        if (finish())
            Dismiss();
    }
}