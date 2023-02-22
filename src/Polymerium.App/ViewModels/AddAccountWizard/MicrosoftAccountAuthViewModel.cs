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
    private readonly IOverlayService _overlayService;

    public MicrosoftAccountAuthViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;
    }

    internal CancellationToken Token { get; set; }

    public async Task LoginDeviceCodeFlowAsync(Action<string?, string?, MicrosoftAccount?> callback)
    {
        var result = await MicrosoftAccount.LoginAsync((code, url) => callback(code, url, null), Token);
        if (result.IsErr(out var exception))
            Error(exception!.Message);
        else
        {
            callback("验证通过", null, result.Unwrap());
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