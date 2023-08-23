using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.ViewModels.Instances;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

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
            Title = ViewModel.Localization.GetString(
                "InstanceAdvancedConfigurationView_Confirm_Title"
            ),
            Text = ViewModel.Localization.GetString(
                "InstanceAdvancedConfigurationView_Confirm_Text"
            )
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            if (ViewModel.DeleteInstance())
                ViewModel.PopNotification(
                    ViewModel.Localization.GetString(
                        "InstanceAdvancedConfigurtationView_Delete_Caption"
                    ),
                    ViewModel.Localization.GetString(
                        "InstanceAdvancedConfigurtationView_Delete_Success_Message"
                    )
                );
            else
                ViewModel.PopNotification(
                    ViewModel.Localization.GetString(
                        "InstanceAdvancedConfigurtationView_Delete_Caption"
                    ),
                    ViewModel.Localization.GetString(
                        "InstanceAdvancedConfigurtationView_Delete_Failure_Message"
                    ),
                    InfoBarSeverity.Error
                );
    }

    private async void ResetInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmationDialog
        {
            XamlRoot = XamlRoot,
            Title = ViewModel.Localization.GetString(
                "InstanceAdvancedConfigurationView_Confirm_Title"
            ),
            Text = ViewModel.Localization.GetString(
                "InstanceAdvancedConfigurationView_Confirm_Text"
            )
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            if (ViewModel.ResetInstance())
                ViewModel.PopNotification(
                    ViewModel.Localization.GetString(
                        "InstanceAdvancedConfigurtationView_Reset_Caption"
                    ),
                    ViewModel.Localization.GetString(
                        "InstanceAdvancedConfigurtationView_Reset_Success_Message"
                    )
                );
            else
                ViewModel.PopNotification(
                    ViewModel.Localization.GetString(
                        "InstanceAdvancedConfigurtationView_Reset_Caption"
                    ),
                    ViewModel.Localization.GetString(
                        "InstanceAdvancedConfigurtationView_Reset_Failure_Message"
                    ),
                    InfoBarSeverity.Error
                );
    }

    private async void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        var instance = ViewModel.Context.AssociatedInstance;
        var dialog = new TextInputDialog();
        dialog.Title = ViewModel.Localization.GetString(
            "InstanceAdvancedConfigurationView_Rename_Title"
        );
        dialog.InputTextPlaceholder = instance!.Name;
        dialog.Description = ViewModel.Localization.GetString(
            "InstanceAdvancedConfigurationView_Rename_Description"
        );
        dialog.XamlRoot = App.Current.Window.Content.XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var result = ViewModel.RenameInstance(dialog.InputText);
            if (result.HasValue)
            {
                var errorDialog = new MessageDialog
                {
                    XamlRoot = App.Current.Window.Content.XamlRoot,
                    Title = ViewModel.Localization.GetString(
                        "InstanceAdvancedConfigurationView_Rename_Title"
                    ),
                    Message = result.Value.ToString()
                };
                await errorDialog.ShowAsync();
            }
        }
    }

    private void DragDropPane_DragEnter(object sender, DragEventArgs e)
    {
        DragDropPane.Opacity = 1.0;
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
            e.AcceptedOperation = DataPackageOperation.Link;
    }

    private void DragDropPane_DragLeave(object sender, DragEventArgs e)
    {
        DragDropPane.Opacity = 0.3;
    }

    private async void DragDropPane_Drop(object sender, DragEventArgs e)
    {
        DragDropPane.Opacity = 0.3;
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var file = items!.First()!;
            e.Handled = true;
            //if (File.Exists(file.Path))
                //await ViewModel.FileAccepted(file.Path, raw => _view.Add(raw));
        }
    }
}
