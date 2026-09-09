using System;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Tasks;
using TridentCore.Core.Engines.Launching;
using TridentCore.Core.Exceptions;
using TridentCore.Core.Services.Instances;

namespace Polymerium.Avalonia.Services.Sinks;

/// <summary>
///     订阅 <see cref="InstanceStateAggregator" />，在活动完成时发通知（成功/失败）。
///     取消和分离不发通知；<see cref="LaunchOutcome.Crashed" /> 的崩溃诊断由
///     <see cref="CrashDiagnosisSink" /> 负责。
/// </summary>
public class NotificationSink(
    InstanceStateAggregator aggregator,
    NotificationService notificationService,
    NavigationService navigationService,
    InstanceService instanceService)
{
    private const string JAVA_DOWNLOAD_URL = "https://adoptium.net/temurin/releases/";

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
            case InstanceActivity.Installing install:
                HandleInstallCompleted(install);
                break;
            case InstanceActivity.Updating update:
                HandleUpdateCompleted(update);
                break;
            case InstanceActivity.Deploying deploy:
                HandleDeployCompleted(deploy);
                break;
            case InstanceActivity.Running running:
                HandleLaunchCompleted(running);
                break;
        }
    }

    private void HandleInstallCompleted(InstanceActivity.Installing tracker)
    {
        switch (tracker.State)
        {
            case ActivityState.Finished:
                notificationService.PopMessage(LanguageManager.Instance.MainWindow_InstanceInstallingSuccessNotificationMessage.Current(),
                                               tracker.Key,
                                               GrowlLevel.Success,
                                               true,
                                               ThumbnailHelper.ForInstance(tracker.Key),
                                               new GrowlAction(LanguageManager.Instance.MainWindow_InstanceInstallingSuccessNotificationOpenText.Current(),
                                                               new RelayCommand(() => navigationService
                                                                                   .Navigate<InstancePage>(tracker
                                                                                       .Key))));
                break;
            case ActivityState.Faulted:
                notificationService.PopMessage(tracker.FailureReason,
                                               LanguageManager.Instance.MainWindow_InstanceInstallingDangerNotificationTitle.Current()
                                                        .Replace("{0}", tracker.Key),
                                               thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
                break;
        }
    }

    private void HandleUpdateCompleted(InstanceActivity.Updating tracker)
    {
        switch (tracker.State)
        {
            case ActivityState.Finished:
                notificationService.PopMessage(LanguageManager.Instance.MainWindow_InstanceUpdatingSuccessNotificationMessage.Current(),
                                               tracker.Key,
                                               GrowlLevel.Success,
                                               true,
                                               ThumbnailHelper.ForInstance(tracker.Key),
                                               new GrowlAction(LanguageManager.Instance.MainWindow_InstanceUpdatingSuccessNotificationOpenText.Current(),
                                                               new RelayCommand(() => navigationService
                                                                                   .Navigate<InstancePage>(tracker
                                                                                       .Key))));
                break;
            case ActivityState.Faulted:
                notificationService.PopMessage(tracker.FailureReason,
                                               LanguageManager.Instance.MainWindow_InstanceUpdatingDangerNotificationTitle.Current()
                                                        .Replace("{0}", tracker.Key),
                                               thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
                break;
        }
    }

    private void HandleDeployCompleted(InstanceActivity.Deploying tracker)
    {
        switch (tracker.State)
        {
            case ActivityState.Finished:
                notificationService.PopMessage(LanguageManager.Instance.MainWindow_InstanceDeployingSuccessNotificationMessage.Current(),
                                               tracker.Key,
                                               GrowlLevel.Success,
                                               thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
                break;
            case ActivityState.Faulted:
                var title = LanguageManager.Instance.MainWindow_InstanceDeployingDangerNotificationTitle.Current().Replace("{0}", tracker.Key);
                if (FindBuildArtifactConflict(tracker.FailureReason) is not null)
                {
                    notificationService.PopMessage(LanguageManager.Instance.MainWindow_InstanceDeployingBuildArtifactConflictDangerNotificationMessage.Current(),
                                                   title,
                                                   GrowlLevel.Danger,
                                                   thumbnail: ThumbnailHelper.ForInstance(tracker.Key),
                                                   actions: new
                                                       GrowlAction(LanguageManager.Instance.MainWindow_InstanceDeployingBuildArtifactConflictResetActionText.Current(),
                                                                   new AsyncRelayCommand(() => instanceService
                                                                      .ResetAsync(tracker.Key))));
                    break;
                }

                notificationService.PopMessage(tracker.FailureReason,
                                               title,
                                               thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
                break;
        }
    }

    private void HandleLaunchCompleted(InstanceActivity.Running tracker)
    {
        switch (tracker.State)
        {
            case ActivityState.Finished when tracker.Outcome is LaunchOutcome.Exited:
                notificationService.PopMessage(LanguageManager.Instance.MainWindow_InstanceLaunchingSuccessNotificationMessage.Current(),
                                               tracker.Key,
                                               GrowlLevel.Success,
                                               thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
                break;
            case ActivityState.Faulted:
                if (tracker.Outcome is LaunchOutcome.Crashed)
                {
                    return;
                }

                if (tracker.FailureReason is JavaNotFoundException javaNotFound)
                {
                    HandleJavaNotFound(tracker.Key, javaNotFound);
                    return;
                }

                notificationService.PopMessage(tracker.FailureReason,
                                               tracker.Key,
                                               thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
                break;
        }
    }

    private static BuildArtifactConflictException? FindBuildArtifactConflict(Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is BuildArtifactConflictException conflict)
            {
                return conflict;
            }

            if (exception is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    if (FindBuildArtifactConflict(inner) is { } found)
                    {
                        return found;
                    }
                }

                return null;
            }

            exception = exception.InnerException;
        }

        return null;
    }

    private void HandleJavaNotFound(string key, JavaNotFoundException exception) =>
        notificationService.PopMessage(string.Format(LanguageManager.Instance.MainWindow_JavaRuntimeNotFoundDangerNotificationMessage.Current(),
                                                     exception.MajorVersion),
                                       LanguageManager.Instance.MainWindow_JavaRuntimeNotFoundDangerNotificationTitle.Current()
                                                .Replace("{0}", key),
                                       GrowlLevel.Danger,
                                       thumbnail: ThumbnailHelper.ForInstance(key),
                                       actions: new
                                           GrowlAction(LanguageManager.Instance.MainWindow_JavaRuntimeNotFoundDangerNotificationDownloadText.Current(),
                                                       new AsyncRelayCommand(() =>
                                                                                 TopLevelHelper
                                                                                    .LaunchUriAsync(TopLevelHelper
                                                                                            .GetTopLevel(),
                                                                                         new(JAVA_DOWNLOAD_URL),
                                                                                         LanguageManager.Instance.MainWindow_JavaRuntimeDownloadDangerNotificationTitle.Current(),
                                                                                         notificationService))));
}
