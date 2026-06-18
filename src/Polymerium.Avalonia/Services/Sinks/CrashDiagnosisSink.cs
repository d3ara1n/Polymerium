using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Tasks;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Exceptions;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using Huskui.Avalonia.Models;

namespace Polymerium.Avalonia.Services.Sinks;

/// <summary>
///     订阅 <see cref="InstanceStateAggregator" />，在 Launch tracker 因
///     <see cref="ProcessFaultedException" /> 失败时发崩溃诊断通知（Danger growl + Diagnose 按钮）。
/// </summary>
public class CrashDiagnosisSink(
    InstanceStateAggregator aggregator,
    NotificationService notificationService,
    ProfileManager profileManager,
    OverlayService overlayService)
{
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
        if (snapshot.Tracker is not LaunchTracker launcher)
        {
            return;
        }

        if (launcher.State != TrackerState.Faulted)
        {
            return;
        }

        if (!IsProcessFaulted(launcher.FailureReason))
        {
            return;
        }

        notificationService.PopMessage(
            Resources.MainWindow_InstanceLaunchingDangerNotificationMessage
                     .Replace("{0}", launcher.Key),
            Resources.MainWindow_InstanceLaunchingDangerNotificationTitle,
            GrowlLevel.Danger,
            thumbnail: ThumbnailHelper.ForInstance(launcher.Key),
            actions:
            [
                new GrowlAction(
                    Resources.MainWindow_InstanceLaunchingDangerNotificationDiagnoseText,
                    new RelayCommand(() => Diagnose(launcher)),
                    null
                ),
            ]);
    }

    private void Diagnose(LaunchTracker tracker)
    {
        var crashReport = BuildCrashReport(tracker);
        var modal = new Modals.GameCrashReportModal { Report = crashReport };
        overlayService.PopModal(modal);
    }

    private CrashReportModel BuildCrashReport(LaunchTracker tracker)
    {
        var profile = profileManager.TryGetImmutable(tracker.Key, out var p) ? p : null;
        var gameDir = PathDef.Default.DirectoryOfBuild(tracker.Key);
        var logPath = Path.Combine(gameDir, "logs", "latest.log");
        var crashReportPath = FindLatestCrashReport(gameDir);

        var loaderLabel = profile?.Setup.Loader != null &&
                          LoaderHelper.TryParse(profile.Setup.Loader, out var loader)
            ? LoaderHelper.ToDisplayLabel(loader.Identity, loader.Version)
            : Resources.Enum_Vanilla;

        var javaVersion = tracker.JavaVersion?.ToString();
        var javaPath = tracker.JavaHome;
        var allocatedMemory = $"{tracker.Options.MaxMemory} MB";

        string? lastLogLines = null;
        try
        {
            if (File.Exists(logPath))
            {
                var lines = File.ReadAllLines(logPath);
                var lastLines = lines.TakeLast(50).ToArray();
                lastLogLines = string.Join(Environment.NewLine, lastLines);
            }
        }
        catch
        {
            // Ignore
        }

        var osDescription = RuntimeInformation.OSDescription;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            osDescription = $"Windows {Environment.OSVersion.Version}";
        }

        var installedMemory =
            $"{double.Round((double)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024 / 1024)} GB";

        var exitCode = tracker.FailureReason is ProcessFaultedException pfe ? pfe.ExitCode : -1;
        var exceptionMessage = tracker.FailureReason?.Message ?? "Unknown error";
        if (tracker.FailureReason is AggregateException { InnerException: not null } ae)
        {
            exceptionMessage = ae.InnerException?.Message ?? "Unknown error";
            if (ae.InnerException is ProcessFaultedException innerPfe)
            {
                exitCode = innerPfe.ExitCode;
            }
        }

        return new()
        {
            InstanceKey = tracker.Key,
            InstanceName = profile?.Name ?? tracker.Key,
            ExitCode = exitCode,
            LaunchTime = tracker.StartedAt,
            CrashTime = DateTimeOffset.Now,
            ExceptionMessage = exceptionMessage,
            MinecraftVersion = profile?.Setup.Version ?? "Unknown",
            LoaderLabel = loaderLabel,
            GameDirectory = gameDir,
            OperatingSystem = osDescription,
            InstalledMemory = installedMemory,
            JavaVersion = javaVersion ?? "Unknown",
            JavaPath = javaPath ?? "Unknown",
            AllocatedMemory = allocatedMemory,
            LogFilePath = File.Exists(logPath) ? logPath : null,
            CrashReportPath = crashReportPath,
            LastLogLines = lastLogLines,
            ModCount = profile?.Setup.Packages.Count ?? 0,
            CommandLine = tracker.CommandLine,
        };
    }

    private static string? FindLatestCrashReport(string gameDir)
    {
        try
        {
            var crashReportsDir = Path.Combine(gameDir, "crash-reports");
            if (!Directory.Exists(crashReportsDir))
            {
                return null;
            }

            return Directory
                 .GetFiles(crashReportsDir, "crash-*.txt")
                 .OrderByDescending(File.GetLastWriteTime)
                 .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsProcessFaulted(Exception? ex) => ex is ProcessFaultedException
                                                           or AggregateException
    {
        InnerException: ProcessFaultedException
    };
}
