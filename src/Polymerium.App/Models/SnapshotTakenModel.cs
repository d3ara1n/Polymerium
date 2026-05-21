using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Humanizer;
using Polymerium.App.Facilities;
using TridentCore.Abstractions.Snapshots;
using TridentCore.Abstractions.Utilities;

namespace Polymerium.App.Models;

public record FileCategoryEntryModel(string Label, int Count, long Size);

public record FilePartitionModel(string Label, int Count, long Size, IReadOnlyList<FileCategoryEntryModel> Categories);

public class SnapshotTakenModel : ModelBase
{
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

    private static readonly FrozenDictionary<string, string> PrimaryAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["live"] = "运行副本",
        ["import"] = "整合包源",
        ["persist"] = "个人数据",
    }.ToFrozenDictionary();

    private static readonly string[] PrimaryOrder = ["live", "import", "persist"];

    #region Direct

    public required (SnapshotInfo Snapshot, IReadOnlyList<ReferenceInfo> References) Metadata
    {
        get;
        init
        {
            field = value;
            Partitions = BuildPartitions(value.References);
        }
    }

    public DateTime TakenAtRaw => Metadata.Snapshot.CreatedAt;

    public int PackageCount => Metadata.Snapshot.PackageCount;

    public int FileCount => Metadata.Snapshot.FileCount;

    public long TotalSize => Metadata.Snapshot.TotalSize;

    public string LoaderLabel => LoaderHelper.ToDisplayLabel(Metadata.Snapshot.Metadata.Loader);

    public string VersionLabel => Metadata.Snapshot.Metadata.Version;

    public IReadOnlyList<FilePartitionModel> Partitions { get; private set; } = [];

    private static IReadOnlyList<FilePartitionModel> BuildPartitions(
        IReadOnlyList<ReferenceInfo> references)
    {
        var buckets = new Dictionary<string, Dictionary<string, (int count, long size)>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var reference in references)
        {
            var span = reference.RelativePath.AsSpan();
            var firstSlash = span.IndexOf('/');
            var primary = firstSlash >= 0 ? span[..firstSlash].ToString() : span.ToString();
            var remainder = firstSlash >= 0 ? span[(firstSlash + 1)..] : [];

            var secondarySlash = remainder.IndexOf('/');
            var secondary = secondarySlash >= 0
                ? remainder[..secondarySlash].ToString()
                : remainder.ToString();

            ref var bucket = ref CollectionsMarshal.GetValueRefOrAddDefault(
                buckets, primary, out _);
            bucket ??= new(StringComparer.OrdinalIgnoreCase);

            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(
                bucket, secondary, out _);
            entry.count++;
            entry.size += reference.Size;
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
