using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class SettingView : Page
{
    public SettingView()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<SettingViewModel>();
    }

    public SettingViewModel ViewModel { get; }
}