using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;
using System.Collections.Generic;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InstanceView : Page
    {
        public InstanceView()
        {
            InitializeComponent();
        }

        public InstanceViewModel ViewModel { get; } = App.ViewModel<InstanceViewModel>();

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

        private async void AddTodoButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog(XamlRoot) { Message = "Describe your task or memo" };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                ViewModel.AddTodo(dialog.Result);
            }
        }

        private async void SwitchAccountButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog =
                new SwitchAccountDialog(XamlRoot, new List<AccountModel>(ViewModel.GetAccountCandidates()));
            if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.Result != null)
            {
                ViewModel.SwitchAccountTo(dialog.Result);
            }
        }
    }
}