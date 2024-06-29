// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml;
using Polymerium.Trident.Accounts;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Polymerium.App.Modals
{
    public sealed partial class MicrosoftAccountWizardModal
    {
        // Using a DependencyProperty as the backing store for FailureMessage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FailureMessageProperty =
            DependencyProperty.Register(nameof(FailureMessage), typeof(string), typeof(MicrosoftAccountWizardModal),
                new PropertyMetadata(string.Empty));

        // Using a DependencyProperty as the backing store for UserCode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserCodeProperty =
            DependencyProperty.Register(nameof(UserCode), typeof(string), typeof(MicrosoftAccountWizardModal),
                new PropertyMetadata(string.Empty));

        // Using a DependencyProperty as the backing store for Username.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UsernameProperty =
            DependencyProperty.Register(nameof(Username), typeof(string), typeof(MicrosoftAccountWizardModal),
                new PropertyMetadata(string.Empty));

        // Using a DependencyProperty as the backing store for FaceUrl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FaceUrlProperty =
            DependencyProperty.Register(nameof(FaceUrl), typeof(string), typeof(MicrosoftAccountWizardModal),
                new PropertyMetadata(string.Empty));


        private readonly AccountManager _accountManager;
        private readonly IHttpClientFactory _clientFactory;

        private readonly CancellationTokenSource source =
            CancellationTokenSource.CreateLinkedTokenSource(App.Current.Token);

        private string deviceCode = string.Empty;
        private MicrosoftAccount? result;
        private string verificationUrl = string.Empty;

        public MicrosoftAccountWizardModal(IHttpClientFactory clientFactory, AccountManager accountManager)
        {
            _clientFactory = clientFactory;
            _accountManager = accountManager;

            InitializeComponent();
        }

        public string Username
        {
            get => (string)GetValue(UsernameProperty);
            set => SetValue(UsernameProperty, value);
        }

        public string FaceUrl
        {
            get => (string)GetValue(FaceUrlProperty);
            set => SetValue(FaceUrlProperty, value);
        }

        public string FailureMessage
        {
            get => (string)GetValue(FailureMessageProperty);
            set => SetValue(FailureMessageProperty, value);
        }


        public string UserCode
        {
            get => (string)GetValue(UserCodeProperty);
            set => SetValue(UserCodeProperty, value);
        }

        private void ModalBase_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    result = await MicrosoftAccount.LoginAsync(_clientFactory, (user, uri) =>
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            verificationUrl = uri?.AbsoluteUri ?? "https://aka.ms/devicecode";
                            UserCode = user;
                            VisualStateManager.GoToState(this, "Linking", true);
                        }), source.Token);
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        // TODO: set AccountModel, ask for finish, then _accountManager.Append(account);
                        FaceUrl = $"https://starlightskins.lunareclipse.studio/render/pixel/{result.Uuid}/face";
                        Username = result.Username;
                        VisualStateManager.GoToState(this, "Shown", true);
                    });
                }
                catch (Exception e)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        FailureMessage = e.Message;
                        VisualStateManager.GoToState(this, "Failed", true);
                    });
                }
            });
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            DataPackage package = new() { RequestedOperation = DataPackageOperation.Copy };
            package.SetText(UserCode);
            Clipboard.SetContent(package);
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            UriFileHelper.OpenInExternal(verificationUrl);
        }

        private void ModalBase_Unloaded(object sender, RoutedEventArgs e)
        {
            source.Cancel();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(result);
            _accountManager.Append(result);
            DismissCommand.Execute(null);
        }
    }
}