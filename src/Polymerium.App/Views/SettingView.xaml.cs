using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views
{
    public sealed partial class SettingView : Page
    {
        public SettingViewModel ViewModel { get; }

        public SettingView()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Provider.GetRequiredService<SettingViewModel>();
        }
    }
}