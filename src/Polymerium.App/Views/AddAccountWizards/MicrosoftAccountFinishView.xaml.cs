using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.ViewModels.AddAccountWizard;
using Polymerium.Core.Accounts;

namespace Polymerium.App.Views.AddAccountWizards;

public sealed partial class MicrosoftAccountFinishView : Page
{
    // Using a DependencyProperty as the backing store for AvatarUrl.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AvatarUrlProperty = DependencyProperty.Register(
        nameof(AvatarUrl),
        typeof(string),
        typeof(MicrosoftAccountFinishView),
        new PropertyMetadata(string.Empty)
    );

    // Using a DependencyProperty as the backing store for Username.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty UsernameProperty = DependencyProperty.Register(
        nameof(Username),
        typeof(string),
        typeof(MicrosoftAccountFinishView),
        new PropertyMetadata(string.Empty)
    );

    private MicrosoftAccount? account;
    private AddAccountWizardStateHandler? handler;

    public MicrosoftAccountFinishView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<MicrosoftAccountFinishViewModel>();
        InitializeComponent();
    }

    public string AvatarUrl
    {
        get => (string)GetValue(AvatarUrlProperty);
        set => SetValue(AvatarUrlProperty, value);
    }

    public string Username
    {
        get => (string)GetValue(UsernameProperty);
        set => SetValue(UsernameProperty, value);
    }

    public MicrosoftAccountFinishViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        (handler, _, var parameter) = ((AddAccountWizardStateHandler, CancellationToken, object?))
            e.Parameter;
        account = (MicrosoftAccount)parameter!;
        Username = account.Nickname;
        AvatarUrl = $"https://minotar.net/helm/{account.UUID}/100.png";
        handler?.Invoke(null, true, Finish);
        base.OnNavigatedTo(e);
    }

    private bool Finish()
    {
        ViewModel.AddAccount(account!);
        return true;
    }
}
