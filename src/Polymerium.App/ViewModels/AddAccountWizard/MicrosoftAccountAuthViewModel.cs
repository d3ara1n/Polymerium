using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Polymerium.App.Dialogs;
using Polymerium.App.Services;
using Polymerium.Core.Accounts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.App.ViewModels.AddAccountWizard;

public class MicrosoftAccountAuthViewModel : ObservableObject
{
    private readonly DispatcherQueue _dispatcher;
    private readonly IOverlayService _overlayService;

    public MicrosoftAccountAuthViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    internal CancellationToken Token { get; set; }

    public async Task LoginDeviceCodeFlowAsync(Action<string?, string?, MicrosoftAccount?> callback)
    {
        var result = await MicrosoftAccount.LoginAsync(
            (code, url) => callback(code, url, null),
            Token
        );
        if (!Token.IsCancellationRequested)
        {
            if (result.IsSuccessful)
                callback("验证通过", null, result.Value);
            else
                Error(
                    result.Error switch
                    {
                        MicrosoftAccount.MicrosoftAccountError.Canceled => "操作取消",
                        MicrosoftAccount.MicrosoftAccountError.OwnershipFailed => "无法验证用户的游戏所有权",
                        MicrosoftAccount.MicrosoftAccountError.ProfileFailed => "获取玩家信息失败",
                        MicrosoftAccount.MicrosoftAccountError.MinecraftAuthenticationFailed
                            => "Minecraft 获取授权时出现网络异常或未授权",
                        MicrosoftAccount.MicrosoftAccountError.XboxAuthenticationFailed
                            => "Xbox Live 获取授权时出现网络异常或未授权",
                        MicrosoftAccount.MicrosoftAccountError.XstsAuthenticationFailed
                            => "Xbox Live XSTS 获取授权时出现网络异常或未授权",
                        MicrosoftAccount.MicrosoftAccountError.MicrosoftAuthenticationFailed
                            => "通过设备码获取微软账号时出现网络异常或超时",
                        _ => throw new ArgumentOutOfRangeException()
                    }
                );
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
