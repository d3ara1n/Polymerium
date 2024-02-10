using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public static readonly DependencyProperty VersionLoadingStateProperty = DependencyProperty.Register(
        nameof(VersionLoadingState), typeof(DataLoadingState), typeof(DesktopView),
        new PropertyMetadata(DataLoadingState.Idle));

    public DesktopView()
    {
        InitializeComponent();
    }

    public DesktopViewModel ViewModel { get; } = App.ViewModel<DesktopViewModel>();

    public DataLoadingState VersionLoadingState
    {
        get => (DataLoadingState)GetValue(VersionLoadingStateProperty);
        set => SetValue(VersionLoadingStateProperty, value);
    }

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

    private void CreateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (VersionLoadingState != DataLoadingState.Loading)
        {
            VersionLoadingState = DataLoadingState.Loading;
            Task.Run(async () =>
            {
                IEnumerable<MinecraftVersionModel> versions;
                try
                {
                    versions = await ViewModel.FetchVersionAsync();
                }
                catch
                {
                    versions = [];
                }

                async void Callback()
                {
                    VersionLoadingState = DataLoadingState.Done;
                    var dialog = new CreateProfileDialog(XamlRoot, versions);
                    if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                        await ViewModel.CreateProfileAsync(dialog.InstanceName, dialog.SelectedVersion,
                            dialog.ThumbnailImage);

                    if (dialog.ThumbnailImage != null) await dialog.ThumbnailImage.DisposeAsync();
                }

                DispatcherQueue.TryEnqueue(Callback);
            });
        }
    }
}