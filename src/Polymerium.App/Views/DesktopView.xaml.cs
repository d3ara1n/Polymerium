using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DesktopView
{
    public DesktopView()
    {
        InitializeComponent();
    }

    public DesktopViewModel ViewModel { get; } = App.ViewModel<DesktopViewModel>();

    private async void ImportButton_OnClick(object sender, RoutedEventArgs e)
    {
        var inputDialog = new DragDropInputDialog(XamlRoot)
            { CaptionText = "Drag and drop", BodyText = "Any modpack file here" };
        if (await inputDialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var path = inputDialog.ResultPath;
            var result = ViewModel.ExtractModpack(path);
            if (result != null)
            {
                var model = new ModpackPreviewModel(result);
                var previewDialog = new ModpackPreviewDialog(XamlRoot, model);
                if (await previewDialog.ShowAsync() == ContentDialogResult.Primary)
                    ViewModel.ApplyExtractedModpack(model);
            }
        }
    }
}