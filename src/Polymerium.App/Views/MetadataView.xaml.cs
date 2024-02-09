using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;
using Trident.Abstractions.Resources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MetadataView : Page
{
    public MetadataView()
    {
        AttachmentView = new AdvancedCollectionView(ViewModel.Attachments)
        {
            Filter = Filter
        };
        InitializeComponent();
    }

    public MetadataViewModel ViewModel { get; } = App.ViewModel<MetadataViewModel>();

    public AdvancedCollectionView AttachmentView { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.OnAttached(e.Parameter);
        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.OnDetached();
        base.OnNavigatedFrom(e);
    }

    private async void AddLayerButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog(XamlRoot)
        {
            Message = "Summarize usage of your new layer",
            Placeholder = "New Layer"
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary) ViewModel.AddLayer(dialog.Result);
    }

    private void AttachmentQueryBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        AttachmentView.Refresh();
    }

    private bool Filter(object obj)
    {
        if (obj is AttachmentModel attachment)
        {
            var query = AttachmentQueryBox.Text;
            if (string.IsNullOrWhiteSpace(query)) return true;
            return attachment.ProjectName.Value.Contains(query, StringComparison.CurrentCultureIgnoreCase);
        }

        return false;
    }
}