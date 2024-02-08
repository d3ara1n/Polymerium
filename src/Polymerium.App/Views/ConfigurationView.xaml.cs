// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ConfigurationView
{
    public ConfigurationView()
    {
        InitializeComponent();
    }

    public ConfigurationViewModel ViewModel { get; } = App.ViewModel<ConfigurationViewModel>();
}