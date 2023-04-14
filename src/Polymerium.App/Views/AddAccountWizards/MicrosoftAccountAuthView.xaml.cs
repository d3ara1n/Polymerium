using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.ViewModels.AddAccountWizard;
using Polymerium.Core.Accounts;

namespace Polymerium.App.Views.AddAccountWizards;

public sealed partial class MicrosoftAccountAuthView : Page
{
    // Using a DependencyProperty as the backing store for DeviceCode.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty UserCodeProperty =
        DependencyProperty.Register(nameof(UserCode), typeof(string), typeof(MicrosoftAccountAuthView),
            new PropertyMetadata(string.Empty));

    // Using a DependencyProperty as the backing store for VerificationUrl.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty VerificationUrlProperty =
        DependencyProperty.Register(nameof(VerificationUrl), typeof(string), typeof(MicrosoftAccountAuthView),
            new PropertyMetadata(null));

    // Using a DependencyProperty as the backing store for IsPending.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsPendingProperty =
        DependencyProperty.Register("IsPending", typeof(bool), typeof(MicrosoftAccountAuthView),
            new PropertyMetadata(false));

    private AddAccountWizardStateHandler? handler;


    public MicrosoftAccountAuthView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<MicrosoftAccountAuthViewModel>();
        InitializeComponent();
    }

    public string UserCode
    {
        get => (string)GetValue(UserCodeProperty);
        set => SetValue(UserCodeProperty, value);
    }


    public string? VerificationUrl
    {
        get => (string?)GetValue(VerificationUrlProperty);
        set => SetValue(VerificationUrlProperty, value);
    }


    public bool IsPending
    {
        get => (bool)GetValue(IsPendingProperty);
        set => SetValue(IsPendingProperty, value);
    }

    public MicrosoftAccountAuthViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        (handler, ViewModel.Token, _) = ((AddAccountWizardStateHandler, CancellationToken, object?))e.Parameter;
        handler?.Invoke(null);
        base.OnNavigatedTo(e);
    }

    private void MicrosoftAccountAuthView_OnLoaded(object sender, RoutedEventArgs e)
    {
        Task.Run(() => ViewModel.LoginDeviceCodeFlowAsync(LoginDeviceCodeFlowHandler));
    }

    private void LoginDeviceCodeFlowHandler(string? userCode, string? verificationUrl,
        MicrosoftAccount? user = null)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UserCode = userCode ?? string.Empty;
            VerificationUrl = verificationUrl;
            if (user != null)
                handler?.Invoke((typeof(MicrosoftAccountFinishView), user));
            else
                handler?.Invoke(null);
        });
    }

    private void OpenBrowserButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (VerificationUrl != null)
        {
            Process.Start(new ProcessStartInfo(VerificationUrl) { UseShellExecute = true });
            IsPending = true;
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(UserCode);
        Clipboard.SetContent(package);
    }
}