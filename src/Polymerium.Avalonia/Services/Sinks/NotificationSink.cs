using System;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Tasks;
using TridentCore.Core.Exceptions;
using TridentCore.Core.Services.Instances;

namespace Polymerium.Avalonia.Services.Sinks;

/// <summary>
///     订阅 <see cref="InstanceStateAggregator" />，在 tracker 完成时发通知（成功/失败/取消）。
///     不处理 ProcessFaultedException 的崩溃诊断（由 <see cref="CrashDiagnosisSink" /> 负责）。
/// </summary>
public class NotificationSink(
    InstanceStateAggregator aggregator,
    NotificationService notificationService,
    NavigationService navigationService,
    InstanceService instanceService)
{
    private const string JAVA_DOWNLOAD_URL = "https://adoptium.net/temurin/releases/";

    public void Attach()
    {
        aggregator.StateChangeStream
                  .Subscribe(change =>
                  {
                      foreach (var item in change)
                      {
                          if (item.Reason is ChangeReason.Remove)
                          {
                              HandleCompleted(item.Current);
                          }
                      }
                  });
    }

    private void HandleCompleted(InstanceStateSnapshot snapshot)
    {
        switch (snapshot.Tracker)
        {
            // Install
            case InstallTracker install:
                HandleInstallCompleted(install);
                break;
            // Update
            case UpdateTracker update:
                HandleUpdateCompleted(update);
                break;
            // Deploy
            case DeployTracker deploy:
                HandleDeployCompleted(deploy);
                break;
            // Launch
            case LaunchTracker launch:
                HandleLaunchCompleted(launch);
                break;
        }
    }

    private void HandleInstallCompleted(InstallTracker tracker)
    {
        switch (tracker.State)
        {
            case TrackerState.Finished:
                notificationService.PopMessage(
                    Resources.MainWindow_InstanceInstallingSuccessNotificationMessage,
                    tracker.Key,
                    GrowlLevel.Success,
                    forceExpire: true,
                    thumbnail: ThumbnailHelper.ForInstance(tracker.Key),
                    actions: new GrowlAction(
                        Resources.MainWindow_InstanceInstallingSuccessNotificationOpenText,
                        new RelayCommand(() => navigationService.Navigate<InstancePage>(tracker.Key)),
                        null
                    ));
                break;
            case TrackerState.Faulted when tracker.FailureReason is not OperationCanceledException:
                notificationService.PopMessage(
                    tracker.FailureReason,
                    Resources.MainWindow_InstanceInstallingDangerNotificationTitle
                             .Replace("{0}", tracker.Key),
                    thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
                break;
        }
    }

    private void HandleUpdateCompleted(UpdateTracker tracker)
    {
        switch (tracker.State)
        {
            case TrackerState.Finished:
                notificationService.PopMessage(
                    Resources.MainWindow_InstanceUpdatingSuccessNotificationMessage,
                    tracker.Key,
                    GrowlLevel.Success,
                    forceExpire: true,
                    thumbnail: ThumbnailHelper.ForInstance(tracker.Key),
                    actions: new GrowlAction(
                        Resources.MainWindow_InstanceUpdatingSuccessNotificationOpenText,
                        new RelayCommand(() => navigationService.Navigate<InstancePage>(tracker.Key)),
                        null
                    ));
                break;
            case TrackerState.Faulted when tracker.FailureReason is not OperationCanceledException:
                notificationService.PopMessage(
                    tracker.FailureReason,
                    Resources.MainWindow_InstanceUpdatingDangerNotificationTitle
                             .Replace("{0}", tracker.Key),
                    thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
                break;
        }
    }

    private void HandleDeployCompleted(DeployTracker tracker)
    {
        switch (tracker.State)
        {
            case TrackerState.Finished:
                notificationService.PopMessage(
                    Resources.MainWindow_InstanceDeployingSuccessNotificationMessage,
                    tracker.Key,
                    GrowlLevel.Success,
                    thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
                break;
            case TrackerState.Faulted when tracker.FailureReason is not OperationCanceledException:
                var title = Resources.MainWindow_InstanceDeployingDangerNotificationTitle
                                     .Replace("{0}", tracker.Key);
                if (FindBuildArtifactConflict(tracker.FailureReason) is not null)
                {
                    notificationService.PopMessage(
                        Resources.MainWindow_InstanceDeployingBuildArtifactConflictDangerNotificationMessage,
                        title,
                        GrowlLevel.Danger,
                        thumbnail: ThumbnailHelper.ForInstance(tracker.Key),
                        actions: new GrowlAction(
                            Resources.MainWindow_InstanceDeployingBuildArtifactConflictResetActionText,
                            new AsyncRelayCommand(() => instanceService.ResetAsync(tracker.Key)),
                            null));
                    break;
                }

                notificationService.PopMessage(
                    tracker.FailureReason,
                    title,
                    thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
                break;
        }
    }

    private void HandleLaunchCompleted(LaunchTracker tracker)
    {
        switch (tracker.State)
        {
            case TrackerState.Finished:
                notificationService.PopMessage(
                    Resources.MainWindow_InstanceLaunchingSuccessNotificationMessage,
                    tracker.Key,
                    GrowlLevel.Success,
                    thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
                break;
            case TrackerState.Faulted when tracker.FailureReason is not OperationCanceledException:
                // ProcessFaultedException 由 CrashDiagnosisSink 处理
                if (IsProcessFaulted(tracker.FailureReason))
                {
                    return;
                }

                if (tracker.FailureReason is JavaNotFoundException javaNotFound)
                {
                    HandleJavaNotFound(tracker.Key, javaNotFound);
                    return;
                }

                // AccountException 或其他错误
                notificationService.PopMessage(
                    tracker.FailureReason,
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

    private static bool IsProcessFaulted(Exception? ex) => ex is ProcessFaultedException
                                                           or AggregateException
    {
        InnerException: ProcessFaultedException
    };

    private void HandleJavaNotFound(string key, JavaNotFoundException exception)
    {
        notificationService.PopMessage(
            string.Format(
                Resources.MainWindow_JavaRuntimeNotFoundDangerNotificationMessage,
                exception.MajorVersion),
            Resources.MainWindow_JavaRuntimeNotFoundDangerNotificationTitle.Replace("{0}", key),
            GrowlLevel.Danger,
            thumbnail: ThumbnailHelper.ForInstance(key),
            actions: new GrowlAction(
                Resources.MainWindow_JavaRuntimeNotFoundDangerNotificationDownloadText,
                new AsyncRelayCommand(() => TopLevelHelper.LaunchUriAsync(
                    TopLevelHelper.GetTopLevel(),
                    new(JAVA_DOWNLOAD_URL),
                    Resources.MainWindow_JavaRuntimeDownloadDangerNotificationTitle,
                    notificationService)),
                null));
    }
}
