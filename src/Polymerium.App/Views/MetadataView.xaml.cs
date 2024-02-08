using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.Dialogs;
using Polymerium.App.ViewModels;

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
        InitializeComponent();
    }

    public MetadataViewModel ViewModel { get; } = App.ViewModel<MetadataViewModel>();

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
}