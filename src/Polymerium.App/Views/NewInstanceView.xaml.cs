// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NewInstanceView : Page
{
    public NewInstanceView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<NewInstanceViewModel>();
        InitializeComponent();
    }

    public NewInstanceViewModel ViewModel { get; }

    private void DragDropPane_OnDragEnter(object sender, DragEventArgs e)
    {
        DragDropPane.Opacity = 1.0;
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
            e.AcceptedOperation = DataPackageOperation.Link;
    }

    private void DragDropPane_OnDragLeave(object sender, DragEventArgs e)
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
            await ViewModel.ArchiveAcceptedAsync(file.Path);
        }
    }
}