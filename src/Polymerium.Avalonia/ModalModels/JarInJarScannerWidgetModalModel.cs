using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;

namespace Polymerium.Avalonia.ModalModels;

public partial class JarInJarScannerWidgetModalModel(
    IViewContext<string> context,
    NotificationService notificationService) : ViewModelBase
{
    public enum ScannerStatus { Idle, Scanning, Empty, Result }

    private readonly string _key = context.GetRequiredParameter();
    private readonly SourceList<HiddenModEntry> _source = new();
    private readonly CompositeDisposable _subscriptions = new();
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    public partial ScannerStatus Status { get; private set; } = ScannerStatus.Idle;

    [ObservableProperty]
    public partial int ScannedCount { get; private set; }

    [ObservableProperty]
    public partial int TotalCount { get; private set; }

    [ObservableProperty]
    public partial string? CurrentFile { get; private set; }

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    public partial int JarFileCount { get; private set; }

    [ObservableProperty]
    public partial int HiddenModCount { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDuplicates))]
    public partial int DuplicateCount { get; private set; }

    public bool HasDuplicates => DuplicateCount > 0;

    [ObservableProperty]
    public partial ReadOnlyObservableCollection<HiddenModEntry>? FilteredView { get; set; }

    private static Func<HiddenModEntry, bool> BuildTextFilter(string? filter) =>
        string.IsNullOrEmpty(filter)
            ? _ => true
            : x => x.ModId.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(x.Name) && x.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

    private (int jarCount, List<HiddenModEntry> entries, HashSet<string> topLevelIds) ScanImpl(
        IProgress<ScanProgress> progress,
        CancellationToken token)
    {
        var modsDir = Path.Combine(PathDef.Default.DirectoryOfBuild(_key), "mods");
        if (!Directory.Exists(modsDir))
        {
            return (0, [], new(StringComparer.OrdinalIgnoreCase));
        }

        var files = Directory.GetFiles(modsDir, "*.jar", SearchOption.TopDirectoryOnly);
        var entries = new List<HiddenModEntry>();
        var topLevelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < files.Length; i++)
        {
            token.ThrowIfCancellationRequested();
            var file = files[i];
            progress.Report(new(i, files.Length, Path.GetFileName(file)));

            try
            {
                using var outerArchive = ZipFile.OpenRead(file);
                var outerMod = AssetModHelper.ParseMetadata(outerArchive);
                if (!string.IsNullOrEmpty(outerMod.ModId))
                {
                    topLevelIds.Add(outerMod.ModId);
                }

                var host = !string.IsNullOrEmpty(outerMod.ModId) ? outerMod.ModId : Path.GetFileName(file);

                foreach (var nested in AssetModHelper.EnumerateEmbeddedJars(outerArchive))
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        using var innerStream = nested.Open();
                        using var innerArchive = new ZipArchive(innerStream, ZipArchiveMode.Read);
                        var innerMod = AssetModHelper.ParseMetadata(innerArchive);
                        if (!string.IsNullOrEmpty(innerMod.ModId))
                        {
                            entries.Add(new()
                            {
                                ModId = innerMod.ModId,
                                Name = !string.IsNullOrEmpty(innerMod.Name)
                                           ? innerMod.Name
                                           : innerMod.ModId,
                                Version = innerMod.Version,
                                Loader = innerMod.LoaderType,
                                Host = host
                            });
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        progress.Report(new(files.Length, files.Length, null));
        return (files.Length, entries, topLevelIds);
    }

    private record ScanProgress(int Scanned, int Total, string? CurrentFile);

    #region 生命周期

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        var text = this.WhenValueChanged(x => x.SearchText).Select(BuildTextFilter);
        _source.Connect().Filter(text).Bind(out var view).Subscribe().DisposeWith(_subscriptions);
        FilteredView = view;
        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _subscriptions.Dispose();
        return Task.CompletedTask;
    }

    #endregion

    #region 命令

    [RelayCommand]
    private async Task ScanAsync()
    {
        _cts = new();
        var token = _cts.Token;

        Status = ScannerStatus.Scanning;
        ScannedCount = 0;
        TotalCount = 0;
        CurrentFile = null;
        SearchText = null;
        _source.Clear();

        var progress = new Progress<ScanProgress>(p =>
        {
            ScannedCount = p.Scanned;
            TotalCount = p.Total;
            CurrentFile = p.CurrentFile;
        });

        try
        {
            var (jarCount, entries, topLevelIds) = await Task.Run(() => ScanImpl(progress, token), token);

            JarFileCount = jarCount;

            var innerDuplicates = entries
                                 .GroupBy(e => e.ModId, StringComparer.OrdinalIgnoreCase)
                                 .Where(g => g.Count() > 1)
                                 .Select(g => g.Key)
                                 .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                entry.Duplicate = innerDuplicates.Contains(entry.ModId) ? HiddenModEntry.DuplicateKind.Inner :
                                  topLevelIds.Contains(entry.ModId) ? HiddenModEntry.DuplicateKind.WithTopLevel :
                                  HiddenModEntry.DuplicateKind.None;
            }

            var sorted = entries.OrderBy(e => e.ModId, StringComparer.OrdinalIgnoreCase).ToList();
            _source.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(sorted);
            });

            HiddenModCount = sorted.Count;
            DuplicateCount = entries.Count(e => e.Duplicate != HiddenModEntry.DuplicateKind.None);

            Status = sorted.Count > 0 ? ScannerStatus.Result : ScannerStatus.Empty;
        }
        catch (OperationCanceledException)
        {
            Status = ScannerStatus.Idle;
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, "JarInJar scan failed");
            Status = ScannerStatus.Idle;
        }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    #endregion
}
