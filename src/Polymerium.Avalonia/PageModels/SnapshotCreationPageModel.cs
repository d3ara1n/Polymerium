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
    #region Constants

    private static readonly FrozenDictionary<string, string> SecondaryAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["mods"] = "模组",
        ["resourcepacks"] = "资源包",
        ["shaderpacks"] = "光影包",
        ["saves"] = "存档",
        ["world"] = "存档",
        ["config"] = "配置",
        ["logs"] = "日志",
        ["crash-reports"] = "崩溃报告",
        ["screenshots"] = "截图",
        ["textures"] = "纹理文件",
        ["libraries"] = "依赖库",
        ["versions"] = "版本文件",
        ["assets"] = "资源文件",
    }.ToFrozenDictionary();

    // NOTE: live 概念保留——运行副本现在是 import 在 build 上的投影，快照引用以 build/ 前缀落地。
    private static readonly FrozenDictionary<string, string> PrimaryAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["build"] = "运行副本",
        ["import"] = "整合包源",
        ["persist"] = "个人数据",
    }.ToFrozenDictionary();

    private static readonly string[] PrimaryOrder = ["build", "import", "persist"];

    #endregion

    #region Direct

    public SnapshotsModalModel.SnapshotContext Context { get; } = context.Parameter!;

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
            notificationService.PopMessage(ex, "Take snapshot failed");
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
        snapshot = snapshot with { Label = !string.IsNullOrEmpty(Label) ? Label : "Untitled", Remark = Remark };

        var committed = new Progress<int>(x =>
        {
            TotalCommitted = x;
        });

        try
        {
            IsCreating = true;
            TotalCommitted = 0;
            await Context.Handle.CommitAsync(snapshot, references, committed);
            notificationService.PopMessage($"{snapshot.Label} has been saved.", "Snapshot created", GrowlLevel.Success);
            Context.BackHandler.Invoke();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, "Create snapshot failed");
        }
        finally
        {
            IsCreating = false;
        }
    }

    #endregion

    #region Partition Building

    private static IReadOnlyList<FilePartitionModel> BuildPartitions(
        IReadOnlyList<ReferenceInfo> references)
    {
        var buckets = new Dictionary<string, Dictionary<string, (int count, long size)>>(
            StringComparer.OrdinalIgnoreCase);
        var outerLookup = buckets.GetAlternateLookup<ReadOnlySpan<char>>();

        foreach (var reference in references)
        {
            var span = reference.RelativePath.AsSpan();
            var firstSlash = span.IndexOfAny('/', '\\');
            var primarySpan = firstSlash >= 0 ? span[..firstSlash] : span;
            var remainder = firstSlash >= 0 ? span[(firstSlash + 1)..] : [];

            var secondarySlash = remainder.IndexOfAny('/', '\\');
            var secondarySpan = secondarySlash >= 0
                ? remainder[..secondarySlash]
                : remainder;

            if (!outerLookup.TryGetValue(primarySpan, out var secondaries))
            {
                secondaries = new(
                                  StringComparer.OrdinalIgnoreCase);
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

        foreach (var primary in PrimaryOrder)
        {
            if (!buckets.TryGetValue(primary, out var secondaries))
                continue;

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
                    categories.Add(new(alias, count, size));
                }
                else
                {
                    otherCount += count;
                    otherSize += size;
                }
            }

            if (otherCount > 0)
                categories.Add(new("其他", otherCount, otherSize));

            var primaryLabel = PrimaryAliases.GetValueOrDefault(primary, primary);
            result.Add(new(primaryLabel, totalCount, totalSize, categories));
        }

        foreach (var (primary, secondaries) in buckets)
        {
            if (PrimaryOrder.Contains(primary, StringComparer.OrdinalIgnoreCase))
                continue;

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
                    categories.Add(new(alias, count, size));
                }
                else
                {
                    otherSecondaryCount += count;
                    otherSecondarySize += size;
                }
            }

            if (otherSecondaryCount > 0)
                categories.Add(new("其他", otherSecondaryCount, otherSecondarySize));

            primaryOtherCount += totalCount;
            primaryOtherSize += totalSize;
            primaryOtherCategories.AddRange(categories);
        }

        if (primaryOtherCount > 0)
            result.Add(new("其他", primaryOtherCount, primaryOtherSize, primaryOtherCategories));

        return result;
    }

    #endregion
}
