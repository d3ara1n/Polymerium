using System;
using DynamicData;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Tasks;
using TridentCore.Core.Services.Instances;

namespace Polymerium.Avalonia.Services.Sinks;

/// <summary>
///     订阅 <see cref="InstanceStateAggregator" />，在活动完成时写入
///     <see cref="PersistenceService" /> 的操作记录（Install/Update）和活动记录（Launch）。
///     纯数据副作用，不涉及 UI。
/// </summary>
public class ActivitySink(InstanceStateAggregator aggregator, PersistenceService persistenceService)
{
    public void Attach() =>
        aggregator.StateChangeStream.Subscribe(change =>
        {
            foreach (var item in change)
            {
                if (item.Reason is ChangeReason.Remove)
                {
                    HandleCompleted(item.Current);
                }
            }
        });

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
            case InstanceActivity.Running running:
                persistenceService.AppendActivity(new()
                {
                    Key = running.Key,
                    AccountId = running.Options.Account?.Uuid ?? string.Empty,
                    DieInPeace = running.State == ActivityState.Finished,
                    Begin =
                        DateTimeHelper.ToPersistedLocalDateTime(running.StartedAt),
                    End = DateTime.Now
                });
                break;
        }
    }
}
