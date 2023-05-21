using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Models;
using Polymerium.App.ViewModels.Instances;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceConfigurationView : Page
{
    public InstanceConfigurationView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<InstanceConfigurationViewModel>();
        InitializeComponent();
    }

    public InstanceConfigurationViewModel ViewModel { get; }

    private void PageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var view = sender as ListView;
        if (view!.SelectedValue is InstanceConfigurationPageModel page)
            SubpageRoot.Navigate(page.Page);
    }
}