using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polymerium.Avalonia.Dialogs;
using TridentCore.Abstractions;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;

namespace Polymerium.Avalonia.Services;

/// <summary>
///     组织退出确认，并为 Core 的停止操作设置应用层等待期限。
/// </summary>
public class ExitGuardService(InstanceManager instanceManager, OverlayService overlayService)
{
    private static readonly TimeSpan SETTLE_GRACE_PERIOD = TimeSpan.FromSeconds(3);

    public bool IsBusy => instanceManager.IsInUse();

    public async Task<bool> RequestConfirmationAsync()
    {
        if (!IsBusy)
        {
            return true;
        }

        var running = 0;
        var working = 0;
        foreach (var activity in instanceManager.CurrentActivities)
        {
            if (activity.Kind is InstanceState.Running)
            {
                running++;
            }
            else
            {
                working++;
            }
        }

        List<string> parts = [];
        if (working > 0)
        {
            parts.Add(string.Format(LanguageManager.Instance.App_ExitConfirmTaskMessage.Current(), working));
        }

        if (running > 0)
        {
            parts.Add(string.Format(LanguageManager.Instance.App_ExitConfirmRunningMessage.Current(), running));
        }

        var dialog = new MessageDialog
        {
            Title = LanguageManager.Instance.App_ExitConfirmTitle.Current(),
            Message = string.Join(Environment.NewLine, parts),
            PrimaryText = LanguageManager.Instance.Dialog_ConfirmButtonText.Current(),
            IsPrimaryButtonVisible = true
        };
        return await overlayService.PopDialogAsync(dialog);
    }

    public async Task StopAsync()
    {
        try
        {
            await instanceManager.StopAsync().WaitAsync(SETTLE_GRACE_PERIOD);
        }
        catch (TimeoutException)
        {
            // The application may exit after the grace period even if Core is still stopping.
        }
    }
}
