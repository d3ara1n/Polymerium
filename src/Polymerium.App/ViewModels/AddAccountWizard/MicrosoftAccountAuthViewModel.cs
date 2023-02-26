using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Polymerium.App.Dialogs;
using Polymerium.App.Services;
using Polymerium.Core.Accounts;

namespace Polymerium.App.ViewModels.AddAccountWizard;

public class MicrosoftAccountAuthViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private readonly DispatcherQueue _dispatcher;

    public MicrosoftAccountAuthViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    internal CancellationToken Token { get; set; }

    public async Task LoginDeviceCodeFlowAsync(Action<string?, string?, MicrosoftAccount?> callback)
    {
        var result = await MicrosoftAccount.LoginAsync((code, url) => callback(code, url, null), Token);
        if (!Token.IsCancellationRequested)
        {
            if (result.IsErr(out var error))
                Error(error!);
            else
                callback("验证通过", null, result.Unwrap());
        }
    }

    private void Error(string reason)
    {
        _dispatcher.TryEnqueue(() =>
        {
            _overlayService.Dismiss();
            var dialog = new MessageDialog
            {
                XamlRoot = App.Current.Window.Content.XamlRoot,
                Title = "登录未完成",
                Message = reason
            };
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
            dialog.ShowAsync();
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
        });
    }
}