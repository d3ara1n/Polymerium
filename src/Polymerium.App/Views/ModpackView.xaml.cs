using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.ViewModels;
using Polymerium.Trident.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views;

public sealed partial class ModpackView : Page
{
    public ModpackView() => InitializeComponent();

    public ModpackViewModel ViewModel { get; } = App.ViewModel<ModpackViewModel>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.OnAttached(e.Parameter);
    }

    private void MarkdownTextBlock_LinkClicked(object sender, LinkClickedEventArgs e) =>
        UriFileHelper.OpenInExternal(e.Link);
}