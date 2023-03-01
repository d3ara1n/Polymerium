using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.ViewModels.AddAccountWizard;

namespace Polymerium.App.Views.AddAccountWizards;

public sealed partial class MicrosoftAccountIntroView : Page
{
    private AddAccountWizardStateHandler? handler;

    public MicrosoftAccountIntroView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<MicrosoftAccountIntroViewModel>();
        InitializeComponent();
    }

    public MicrosoftAccountIntroViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        (handler, _, _) = ((AddAccountWizardStateHandler, CancellationToken, object?))e.Parameter;
        handler?.Invoke((typeof(MicrosoftAccountAuthView), null), false, Finish);
        base.OnNavigatedTo(e);
    }

    private bool Finish()
    {
        return false;
    }
}