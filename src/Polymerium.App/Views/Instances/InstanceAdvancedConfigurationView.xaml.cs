using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.ViewModels.Instances;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceAdvancedConfigurationView : Page
{
    public InstanceAdvancedConfigurationView()
    {
        ViewModel =
            App.Current.Provider.GetRequiredService<InstanceAdvancedConfigurationViewModel>();
        InitializeComponent();
    }

    public InstanceAdvancedConfigurationViewModel ViewModel { get; }

    private async void DeleteInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmationDialog
        {
            XamlRoot = XamlRoot,
            Title = "Really?",
            Text = "�˲������ɳ���"
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary) ViewModel.DeleteInstance();
    }

    private async void ResetInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmationDialog
        {
            XamlRoot = XamlRoot,
            Title = "Really?",
            Text = "�˲������ɳ���"
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary) ViewModel.ResetInstance();
    }
}