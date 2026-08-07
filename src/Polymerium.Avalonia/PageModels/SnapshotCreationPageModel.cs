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

    #region Partitions

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
        var otherLabel = "InstanceStoragePage_OtherLabelText";

        foreach (var primary in PRIMARY_ORDER)
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
                if (SECONDARY_ALIASES.TryGetValue(key, out var alias))
                {
                    categories.Add(new(alias, count, size));
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

            var primaryLabel = PRIMARY_ALIASES.GetValueOrDefault(primary, primary);
            result.Add(new(primaryLabel, totalCount, totalSize, categories));
        }

        foreach (var (primary, secondaries) in buckets)
        {
            if (PRIMARY_ORDER.Contains(primary, StringComparer.OrdinalIgnoreCase))
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
                if (SECONDARY_ALIASES.TryGetValue(key, out var alias))
                {
                    categories.Add(new(alias, count, size));
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

    private static readonly FrozenDictionary<string, string> SECONDARY_ALIASES =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["mods"] = "AssetKind_Mod",
            ["resourcepacks"] = "AssetKind_ResourcePack",
            ["shaderpacks"] = "AssetKind_ShaderPack",
            ["saves"] = "AssetKind_Save",
            ["world"] = "AssetKind_Save",
            ["config"] = "AssetKind_Config",
            ["logs"] = "AssetKind_Log",
            ["crash-reports"] = "AssetKind_CrashReport",
            ["screenshots"] = "AssetKind_Screenshot",
            ["textures"] = "AssetKind_Texture",
            ["libraries"] = "AssetKind_Library",
            ["versions"] = "AssetKind_Version",
            ["assets"] = "AssetKind_Asset"
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, string> PRIMARY_ALIASES =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["build"] = "InstanceStoragePage_BuildFolderLinkText",
            ["import"] = "InstanceStoragePage_ImportFolderLinkText",
            ["persist"] = "InstanceStoragePage_PersistFolderLinkText"
        }.ToFrozenDictionary();

    private static readonly string[] PRIMARY_ORDER = ["build", "import", "persist"];

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
            notificationService.PopMessage(ex, LanguageManager.Instance.SnapshotCreationPage_TakeDangerNotificationTitle.Current());
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
            Label = !string.IsNullOrEmpty(Label) ? Label : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
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
            notificationService.PopMessage(LanguageManager.Instance.SnapshotCreationPage_CreateSuccessNotificationMessage.Current()
                                                    .Replace("{0}", snapshot.Label),
                                           LanguageManager.Instance.SnapshotCreationPage_CreateSuccessNotificationTitle.Current(),
                                           GrowlLevel.Success);
            Context.BackHandler.Invoke();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, LanguageManager.Instance.SnapshotCreationPage_CreateDangerNotificationTitle.Current());
        }
        finally
        {
            IsCreating = false;
        }
    }

    #endregion
}
