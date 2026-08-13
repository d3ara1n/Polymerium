using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Adapters;
using TridentCore.Core.Services;

namespace Polymerium.Avalonia.ModalModels;

public partial class MigrateModalModel(
    MigratorAgent migratorAgent,
    NotificationService notificationService) : ViewModelBase
{
    private IDisposable? _selectionSubscription;

    public IReadOnlyList<MigrateLauncherKindModel> Kinds { get; } =
    [
        .. migratorAgent.SupportedKinds.Select(k => new MigrateLauncherKindModel(k,
                                                   migratorAgent.DefaultDataDirectory(k)))
    ];

    [ObservableProperty]
    public partial MigrateLauncherKindModel? SelectedLauncher { get; set; }

    [ObservableProperty]
    public partial string? DataDirectory { get; set; }

    [ObservableProperty]
    public partial bool IsScanning { get; private set; }

    [ObservableProperty]
    public partial MigrateScanResult? Result { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSummary))]
    [NotifyPropertyChangedFor(nameof(MigrateLabel))]
    [NotifyCanExecuteChangedFor(nameof(MigrateCommand))]
    public partial int SelectedCount { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSummary))]
    public partial int TotalCount { get; private set; }

    [ObservableProperty]
    public partial bool? AllSelected { get; set; }

    public string SelectedSummary =>
        string.Format(LanguageManager.Instance.MigrateModal_SelectedCountFormat.Current(), SelectedCount, TotalCount);

    public string MigrateLabel => string.Format(LanguageManager.Instance.MigrateModal_MigrateWithCount.Current(), SelectedCount);

    partial void OnAllSelectedChanged(bool? value)
    {
        if (value is not { } v || Result is null)
        {
            return;
        }

        foreach (var item in Result.Instances.Where(x => x.Instance.CorruptReason is null))
        {
            item.IsSelected = v;
        }
    }

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        SelectedLauncher = Kinds.FirstOrDefault();
        return Task.CompletedTask;
    }

    partial void OnSelectedLauncherChanged(MigrateLauncherKindModel? value) => DataDirectory = value?.DefaultDirectory;

    protected override Task OnDeinitializeAsync()
    {
        _selectionSubscription?.Dispose();
        return Task.CompletedTask;
    }

    private void ArmSelectionPipeline()
    {
        _selectionSubscription?.Dispose();
        _selectionSubscription = Result
                               ?.Instances.ToObservableChangeSet()
                                .AutoRefresh(x => x.IsSelected)
                                .Subscribe(_ => RefreshSelectionState());
    }

    private void RefreshSelectionState()
    {
        if (Result is null)
        {
            return;
        }

        var instances = Result.Instances;
        TotalCount = instances.Count;
        SelectedCount = instances.Count(x => x.IsSelected);
        var selectable = instances.Count(x => x.Instance.CorruptReason is null);
        AllSelected = SelectedCount == 0 ? false : SelectedCount == selectable ? true : null;
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        var top = TopLevelHelper.GetTopLevel();
        if (!top.StorageProvider.CanOpen)
        {
            return;
        }

        var folders = await top.StorageProvider.OpenFolderPickerAsync(new() { Title = LanguageManager.Instance.MigrateModal_Title.Current() });
        var path = folders.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            DataDirectory = path;
        }
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (SelectedLauncher is null || string.IsNullOrWhiteSpace(DataDirectory) || !Directory.Exists(DataDirectory))
        {
            notificationService.PopMessage(LanguageManager.Instance.Migrate_DirectoryMissing.Current(), LanguageManager.Instance.Migrate_Title.Current());
            return;
        }

        IsScanning = true;
        Result = null;
        try
        {
            var instances = await migratorAgent.ScanAsync(SelectedLauncher.Kind,
                                                          DataDirectory!,
                                                          CancellationToken.None);
            Result = new()
            {
                Instances =
                [
                    .. instances.Select(i => new MigrateInstanceModel(i)
                    {
                        IsSelected = i.CorruptReason is null
                    })
                ]
            };
            ArmSelectionPipeline();
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, LanguageManager.Instance.Migrate_ScanFailed.Current());
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        Result = null;
        _selectionSubscription?.Dispose();
        _selectionSubscription = null;
    }

    private bool CanMigrate() => SelectedCount > 0;

    [RelayCommand(CanExecute = nameof(CanMigrate))]
    private async Task MigrateAsync(Modal? self)
    {
        var selected = Result?.Instances.Where(x => x.IsSelected).Select(x => x.Instance).ToList();
        if (selected is null || selected.Count == 0 || self is null)
        {
            return;
        }

        var total = selected.Count;
        using var cts = new CancellationTokenSource();
        var handle = notificationService.PopProgress(LanguageManager.Instance.Migrate_Preparing.Current(), LanguageManager.Instance.Migrate_Title.Current());
        // NOTE: cancel-only action — it cancels the migrate CTS but keeps the notification visible so
        //  the user sees progress until the summary lands.
        handle.AddAction(new(LanguageManager.Instance.Dialog_CancelButtonText.Current(), new RelayCommand(cts.Cancel)));
        self.Dismiss();

        var progress = new Progress<MigrateProgress>(p =>
        {
            if (p.CurrentPhase == MigrateProgress.Phase.Identifying)
            {
                handle.Report(LanguageManager.Instance.Migrate_Identifying.Current());
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
                summary = string.Format(LanguageManager.Instance.Migrate_SummarySuccess.Current(), succeeded);
                level = GrowlLevel.Success;
            }
            else
            {
                var failedList = failed.Count > 0
                                     ? "\n" + string.Join("\n", failed.Select(f => $"- {f.Name}: {f.Failure}"))
                                     : string.Empty;
                summary = string.Format(LanguageManager.Instance.Migrate_SummaryPartial.Current(), succeeded, total, failedList);
                level = succeeded > 0 ? GrowlLevel.Warning : cancelled ? GrowlLevel.Information : GrowlLevel.Danger;
            }

            notificationService.PopMessage(summary, LanguageManager.Instance.Migrate_Title.Current(), level);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, LanguageManager.Instance.Migrate_Failed.Current());
        }
        finally
        {
            handle.Dispose();
        }
    }
}
