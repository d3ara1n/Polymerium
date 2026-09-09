using System;
using System.Reactive.Linq;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Tasks;
using TridentCore.Core.Engines.Launching;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;

namespace Polymerium.Avalonia.Services.Sinks;

/// <summary>
///     订阅 <see cref="InstanceManager.Activities" />，在活动完成时写入
///     <see cref="PersistenceService" /> 的操作记录（Install/Update）和活动记录（Launch）。
///     纯数据副作用，不涉及 UI。
/// </summary>
public class ActivitySink(InstanceManager instanceManager, PersistenceService persistenceService)
{
    public void Attach() =>
        instanceManager.Activities.Where(activity => activity.IsCompleted).Subscribe(HandleCompleted);

    private void HandleCompleted(InstanceActivity activity)
    {
        switch (activity)
        {
            case InstanceActivity.Installing { State: ActivityState.Finished } install:
                persistenceService.AppendAction(new()
                {
                    Key = install.Key,
                    Kind = PersistenceService.ActionKind.Install,
                    New = install.Reference
                });
                break;
            case InstanceActivity.Updating { State: ActivityState.Finished } update:
                persistenceService.AppendAction(new()
                {
                    Key = update.Key,
                    Kind = PersistenceService.ActionKind.Update,
                    Old = update.OldSource,
                    New = update.NewSource
                });
                break;
            case InstanceActivity.Running { RunStartedAt: { } started, CompletedAt: { } completed } running:
                persistenceService.AppendActivity(new()
                {
                    Key = running.Key,
                    AccountId = running.AccountId ?? string.Empty,
                    Outcome = running.Outcome ?? LaunchOutcome.Unknown,
                    DieInPeace = running.Outcome is not LaunchOutcome.Crashed,
                    Begin = DateTimeHelper.ToPersistedLocalDateTime(started),
                    End = DateTimeHelper.ToPersistedLocalDateTime(completed)
                });
                break;
        }
    }
}
