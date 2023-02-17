using System;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth.Microsoft.MsalClient;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Polymerium.App.Dialogs;
using Polymerium.App.Services;
using Polymerium.Core.Accounts;

namespace Polymerium.App.ViewModels.AddAccountWizard;

public class MicrosoftAccountAuthViewModel : ObservableObject
{
    private const string DEVICE_ENDPOINT = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
    private const string TOKEN_ENDPOINT = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    private const string CLIENT_ID = "66b049dc-22a1-4fd8-a17d-2ccd01332101";
    private const string SCOPE = "XboxLive.signin offline_access";

    private readonly IOverlayService _overlayService;

    public MicrosoftAccountAuthViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;
    }

    internal CancellationToken Token { get; set; }

    public async Task LoginDeviceCodeFlowAsync(Action<string?, string?, MicrosoftAccount?> callback)
    {
        var app = MsalMinecraftLoginHelper.CreateDefaultApplicationBuilder(CLIENT_ID)
            .Build();
        var handler = new LoginHandlerBuilder().ForJavaEdition()
            .WithMsalOAuth(app, factory => factory.CreateDeviceCodeApi(code =>
            {
                callback(code.UserCode, code.VerificationUrl, null);
                return Task.CompletedTask;
            }))
            .Build();
        try
        {
            var session = await handler.LoginFromOAuth(Token);
            var account = new MicrosoftAccount(Guid.NewGuid().ToString(), session.GameSession.UUID!,
                session.GameSession.Username!, session.GameSession.AccessToken!, session.GameSession.ClientToken!);
            callback("验证通过", null, account);
        }
        catch (Exception e)
        {
            Error(e.Message);
        }
    }

    private void Error(string reason)
    {
        DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
        {
            _overlayService.Dismiss();
            var dialog = new MessageDialog
            {
                XamlRoot = App.Current.Window.Content.XamlRoot,
                Title = "登录未完成",
                Message = reason
            };
            dialog.ShowAsync().AsTask().Wait();
        });
    }
}