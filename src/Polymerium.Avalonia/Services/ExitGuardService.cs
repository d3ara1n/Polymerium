using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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

    public async Task SettleBusyActivitiesAsync()
    {
        var activities = instanceManager.CurrentActivities;
        if (activities.Count == 0)
        {
            return;
        }

        // NOTE: IsInUse 是先移除后落终态的，轮询它变 false 不代表终态（及下游同步写入的会话记录）已发生，
        //  故在触发中止前订阅活动流，等这些 key 的终值本身。
        var pending = activities.Select(x => x.Key).ToHashSet();
        var settled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = instanceManager.Activities
           .Where(x => x.IsCompleted && pending.Contains(x.Key))
           .Subscribe(x =>
            {
                pending.Remove(x.Key);
                if (pending.Count == 0)
                {
                    settled.TrySetResult();
                }
            });

        // 订阅建立前已自然结束的活动收不到终值，但其终态在订阅前同步发出即已同步落库，无需等待。
        pending.RemoveWhere(key => !instanceManager.IsInUse(key));
        if (pending.Count == 0)
        {
            return;
        }

        // NOTE: 运行中的游戏只能 Detach——Abort 会杀掉游戏进程；Detach 后会话记录与进程句柄都正常收尾。
        foreach (var activity in activities)
        {
            if (activity is InstanceActivity.Running)
            {
                instanceManager.Detach(activity.Key);
            }
            else
            {
                instanceManager.Abort(activity.Key);
            }
        }

        await Task.WhenAny(settled.Task, Task.Delay(SETTLE_GRACE_PERIOD));
    }
}
