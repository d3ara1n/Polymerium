using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polymerium.Avalonia.Dialogs;
using TridentCore.Abstractions;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;

namespace Polymerium.Avalonia.Services;

/// <summary>
///     在关闭或退出前向 <see cref="InstanceManager" /> 查询忙碌状态并组织确认；确认退出后分流收尾——
///     运行中的游戏 Detach（不杀进程、正常落会话记录），其余活动 Abort——并等待终态落地再放行。
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

    // 终态落地与宽限部由 InstanceManager.SettleAsync 保证；宽限时长是应用层策略，留在此处。
    public Task SettleBusyActivitiesAsync() => instanceManager.SettleAsync(SETTLE_GRACE_PERIOD);
}
