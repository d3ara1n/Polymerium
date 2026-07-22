using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.ModalModels;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using TridentCore.Abstractions.Snapshots;

namespace Polymerium.Avalonia.PageModels;

public partial class SnapshotCreationPageModel(
    IViewContext<SnapshotsModalModel.SnapshotContext> context,
    NotificationService notificationService) : ViewModelBase
{
    #region Direct

    public SnapshotsModalModel.SnapshotContext Context { get; } = context.Parameter!;

    #endregion

    #region Partition Building

    private static IReadOnlyList<FilePartitionModel> BuildPartitions(IReadOnlyList<ReferenceInfo> references)
    {
        var buckets =
            new Dictionary<string, Dictionary<string, (int count, long size)>>(StringComparer.OrdinalIgnoreCase);
        var outerLookup = buckets.GetAlternateLookup<ReadOnlySpan<char>>();

        foreach (var reference in references)
        {
            var span = reference.RelativePath.AsSpan();
            var firstSlash = span.IndexOfAny('/', '\\');
            var primarySpan = firstSlash >= 0 ? span[..firstSlash] : span;
            var remainder = firstSlash >= 0 ? span[(firstSlash + 1)..] : [];

            var secondarySlash = remainder.IndexOfAny('/', '\\');
            var secondarySpan = secondarySlash >= 0 ? remainder[..secondarySlash] : remainder;

            if (!outerLookup.TryGetValue(primarySpan, out var secondaries))
            {
                secondaries = new(StringComparer.OrdinalIgnoreCase);
                outerLookup[primarySpan] = secondaries;
            }

            var innerLookup = secondaries.GetAlternateLookup<ReadOnlySpan<char>>();
            if (innerLookup.TryGetValue(secondarySpan, out var existing))
            {
                innerLookup[secondarySpan] = (existing.count + 1, existing.size + reference.Size);
            }
            else
            {
                innerLookup[secondarySpan] = (1, reference.Size);
            }
        }

        var result = new List<FilePartitionModel>();
        var primaryOtherCount = 0;
        var primaryOtherSize = 0L;
        var primaryOtherCategories = new List<FileCategoryEntryModel>();
        var otherLabel = Resources.InstanceStoragePage_OtherLabelText;

        foreach (var primary in PrimaryOrder)
        {
            if (!buckets.TryGetValue(primary, out var secondaries))
            {
                continue;
            }

            var totalCount = 0;
            var totalSize = 0L;
            var categories = new List<FileCategoryEntryModel>();
            var otherCount = 0;
            var otherSize = 0L;

            foreach (var (key, (count, size)) in secondaries.OrderByDescending(x => x.Value.size))
            {
                totalCount += count;
                totalSize += size;
                if (SecondaryAliases.TryGetValue(key, out var alias))
                {
                    categories.Add(new(alias(), count, size));
                }
                else
                {
                    otherCount += count;
                    otherSize += size;
                }
            }

            if (otherCount > 0)
            {
                categories.Add(new(otherLabel, otherCount, otherSize));
            }

            var primaryLabel = PrimaryAliases.TryGetValue(primary, out var primaryAlias) ? primaryAlias() : primary;
            result.Add(new(primaryLabel, totalCount, totalSize, categories));
        }

        foreach (var (primary, secondaries) in buckets)
        {
            if (PrimaryOrder.Contains(primary, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var totalCount = 0;
            var totalSize = 0L;
            var categories = new List<FileCategoryEntryModel>();
            var otherSecondaryCount = 0;
            var otherSecondarySize = 0L;

            foreach (var (key, (count, size)) in secondaries.OrderByDescending(x => x.Value.size))
            {
                totalCount += count;
                totalSize += size;
                if (SecondaryAliases.TryGetValue(key, out var alias))
                {
                    categories.Add(new(alias(), count, size));
                }
                else
                {
                    otherSecondaryCount += count;
                    otherSecondarySize += size;
                }
            }

            if (otherSecondaryCount > 0)
            {
                categories.Add(new(otherLabel, otherSecondaryCount, otherSecondarySize));
            }

            primaryOtherCount += totalCount;
            primaryOtherSize += totalSize;
            primaryOtherCategories.AddRange(categories);
        }

        if (primaryOtherCount > 0)
        {
            result.Add(new(otherLabel, primaryOtherCount, primaryOtherSize, primaryOtherCategories));
        }

        return result;
    }

    #endregion

    #region Constants

    private static readonly FrozenDictionary<string, Func<string>> SecondaryAliases =
        new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["mods"] = () => Resources.AssetKind_Mod,
            ["resourcepacks"] = () => Resources.AssetKind_ResourcePack,
            ["shaderpacks"] = () => Resources.AssetKind_ShaderPack,
            ["saves"] = () => Resources.AssetKind_Save,
            ["world"] = () => Resources.AssetKind_Save,
            ["config"] = () => Resources.AssetKind_Config,
            ["logs"] = () => Resources.AssetKind_Log,
            ["crash-reports"] = () => Resources.AssetKind_CrashReport,
            ["screenshots"] = () => Resources.AssetKind_Screenshot,
            ["textures"] = () => Resources.AssetKind_Texture,
            ["libraries"] = () => Resources.AssetKind_Library,
            ["versions"] = () => Resources.AssetKind_Version,
            ["assets"] = () => Resources.AssetKind_Asset
        }.ToFrozenDictionary();

    // NOTE: live 概念保留——运行副本现在是 import 在 build 上的投影，快照引用以 build/ 前缀落地。
    private static readonly FrozenDictionary<string, Func<string>> PrimaryAliases =
        new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["build"] = () => Resources.InstanceStoragePage_BuildFolderLinkText,
            ["import"] = () => Resources.InstanceStoragePage_ImportFolderLinkText,
            ["persist"] = () => Resources.InstanceStoragePage_PersistFolderLinkText
        }.ToFrozenDictionary();

    private static readonly string[] PrimaryOrder = ["build", "import", "persist"];

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsSnapshotTaking { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    public partial bool IsCreating { get; set; }

    [ObservableProperty]
    public partial SnapshotTakenModel? SnapshotTaken { get; set; }

    [ObservableProperty]
    public partial int TotalCollected { get; set; }

    [ObservableProperty]
    public partial int TotalProcessed { get; set; }

    [ObservableProperty]
    public partial int TotalCommitted { get; set; }

    [ObservableProperty]
    public partial string Label { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Remark { get; set; } = string.Empty;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task TakeAsync()
    {
        try
        {
            IsSnapshotTaking = true;
            var collected = new Progress<int>(x => TotalCollected = x);
            var processed = new Progress<int>(x => TotalProcessed = x);
            var metadata = await Context.Handle.TakeAsync(collected, processed);
            var partitions = await Task.Run(() => BuildPartitions(metadata.References));
            SnapshotTaken = new() { Metadata = metadata, Partitions = partitions };
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, Resources.SnapshotCreationPage_TakeDangerNotificationTitle);
        }
        finally
        {
            IsSnapshotTaking = false;
        }
    }

    private bool CanCreate(SnapshotTakenModel? model) => model != null && !IsCreating;

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateAsync(SnapshotTakenModel? model)
    {
        if (model == null)
        {
            return;
        }

        var (snapshot, references) = model.Metadata;
        snapshot = snapshot with
        {
            Label = !string.IsNullOrEmpty(Label) ? Label : Resources.Snapshot_UntitledLabelText,
            Remark = Remark
        };

        var committed = new Progress<int>(x =>
        {
            TotalCommitted = x;
        });

        try
        {
            IsCreating = true;
            TotalCommitted = 0;
            await Context.Handle.CommitAsync(snapshot, references, committed);
            notificationService.PopMessage(Resources.SnapshotCreationPage_CreateSuccessNotificationMessage
                                                    .Replace("{0}", snapshot.Label),
                                           Resources.SnapshotCreationPage_CreateSuccessNotificationTitle,
                                           GrowlLevel.Success);
            Context.BackHandler.Invoke();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, Resources.SnapshotCreationPage_CreateDangerNotificationTitle);
        }
        finally
        {
            IsCreating = false;
        }
    }

    #endregion
}
