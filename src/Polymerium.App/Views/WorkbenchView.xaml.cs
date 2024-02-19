using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.ViewModels;
using Polymerium.Trident.Repositories;
using System;
using Trident.Abstractions.Resources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WorkbenchView : Page
    {
        public WorkbenchView()
        {
            InitializeComponent();
        }

        public WorkbenchViewModel ViewModel { get; } = App.ViewModel<WorkbenchViewModel>();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.OnAttached(e.Parameter);
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            ViewModel.OnDetached();
            base.OnNavigatingFrom(e);
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            ViewModel.UpdateSource(RepositoryLabels.CURSEFORGE, args.QueryText, KindBox.SelectedIndex switch
            {
                0 => ResourceKind.Mod,
                1 => ResourceKind.ResourcePack,
                2 => ResourceKind.ShaderPack,
                _ => throw new NotImplementedException()
            });
        }
    }
}