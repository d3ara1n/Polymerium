using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Adapters;
using TridentCore.Core.Services;

namespace Polymerium.Avalonia.ModalModels;

public partial class MigrateModalModel(
    MigratorAgent migratorAgent,
    NotificationService notificationService) : ViewModelBase
{
    private CancellationTokenSource? _scanCts;

    public LauncherKind[] Kinds => migratorAgent.SupportedKinds;

    [ObservableProperty]
    public partial LauncherKind SelectedKind { get; set; }

    [ObservableProperty]
    public partial string? DataDirectory { get; set; }

    [ObservableProperty]
    public partial bool IsScanning { get; private set; }

    [ObservableProperty]
    public partial MigrateScanResult? Result { get; set; }

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        SelectedKind = Kinds.FirstOrDefault();
        DataDirectory = migratorAgent.DefaultDataDirectory(SelectedKind);
        return Task.CompletedTask;
    }

    partial void OnSelectedKindChanged(LauncherKind value) =>
        DataDirectory = migratorAgent.DefaultDataDirectory(value);

    protected override Task OnDeinitializeAsync()
    {
        // Only the coarse-scan token is owned here; the migrate task owns its own CTS and outlives this
        // modal by design, so it must NOT be cancelled on deinitialize.
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = null;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        var top = TopLevelHelper.GetTopLevel();
        if (top.StorageProvider.CanOpen != true)
        {
            return;
        }

        var folders = await top.StorageProvider
                               .OpenFolderPickerAsync(new FolderPickerOpenOptions
                               {
                                   Title = Resources.MigrateModal_Title
                               });
        var path = folders.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            DataDirectory = path;
        }
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (string.IsNullOrWhiteSpace(DataDirectory) || !Directory.Exists(DataDirectory))
        {
            notificationService.PopMessage(Resources.Migrate_DirectoryMissing, Resources.Migrate_Title);
            return;
        }

        _scanCts?.Cancel();
        _scanCts = new();
        IsScanning = true;
        Result = null;
        try
        {
            var instances = await migratorAgent.ScanAsync(SelectedKind, DataDirectory!, _scanCts.Token);
            Result = new MigrateScanResult
            {
                Instances = [..instances.Select(i => new MigrateInstanceRow(i))],
                MigrateCommand = MigrateCommand,
                BackCommand = BackCommand
            };
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, Resources.Migrate_ScanFailed);
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void Back() => Result = null;

    [RelayCommand]
    private async Task MigrateAsync(Modal? self)
    {
        var selected = Result?.Instances.Where(x => x.IsSelected).Select(x => x.Instance).ToList();
        if (selected is null || selected.Count == 0 || self is null)
        {
            return;
        }

        var total = selected.Count;
        using var cts = new CancellationTokenSource();
        var handle = notificationService.PopProgress(Resources.Migrate_Preparing, Resources.Migrate_Title);
        // Cancel-only action: cancels the migrate CTS but keeps the notification visible so the user
        // sees progress until the summary lands. handle.Dispose happens in finally below.
        handle.AddAction(new GrowlAction(Resources.Migrate_CancelButton, new RelayCommand(() => cts.Cancel())));
        self.Dismiss();

        var progress = new Progress<MigrateProgress>(p =>
        {
            if (p.CurrentPhase == MigrateProgress.Phase.Identifying)
            {
                handle.Report(Resources.Migrate_Identifying);
            }
            else
            {
                handle.Report($"{p.InstanceName} [{p.InstanceIndex}/{p.InstanceTotal}]");
                handle.Report((p.Percent ?? 0) * 100);
            }
        });

        try
        {
            var result = await migratorAgent.MigrateAsync(selected, progress, cts.Token);
            var succeeded = result.Entries.Count(e => e.Succeeded);
            var failed = result.Entries.Where(e => !e.Succeeded).ToList();
            var cancelled = cts.IsCancellationRequested;
            string summary;
            GrowlLevel level;
            if (failed.Count == 0 && !cancelled)
            {
                summary = string.Format(Resources.Migrate_SummarySuccess, succeeded);
                level = GrowlLevel.Success;
            }
            else
            {
                var failedList = failed.Count > 0
                    ? "\n" + string.Join("\n", failed.Select(f => $"- {f.Name}: {f.Failure}"))
                    : string.Empty;
                summary = string.Format(Resources.Migrate_SummaryPartial, succeeded, total, failedList);
                level = succeeded > 0
                    ? GrowlLevel.Warning
                    : cancelled ? GrowlLevel.Information : GrowlLevel.Danger;
            }

            notificationService.PopMessage(summary, Resources.Migrate_Title, level);
        }
        catch (OperationCanceledException)
        {
            notificationService.PopMessage(Resources.Migrate_Cancelled, Resources.Migrate_Title);
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, Resources.Migrate_Failed);
        }
        finally
        {
            handle.Dispose();
        }
    }
}
