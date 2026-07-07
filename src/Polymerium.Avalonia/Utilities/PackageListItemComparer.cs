using System.Collections.Generic;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Utilities;

// sorter 负责形状：组按 SourceOrders 档位排（散装恒末），组内 Header<Entry，再按 PersistentIndex。
public sealed class PackageListItemComparer(IList<string> sourceOrders) : IComparer<PackageListItemBase>
{
    public int Compare(PackageListItemBase? x, PackageListItemBase? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return 1;

        var c = CompareGroup(x.Group, y.Group);
        if (c != 0)
            return c;

        c = RankOf(x).CompareTo(RankOf(y));
        if (c != 0)
            return c;

        return IntraIndexOf(x).CompareTo(IntraIndexOf(y));
    }

    private int CompareGroup(GroupModelBase x, GroupModelBase y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        var xLoose = x is LooseGroupModel;
        var yLoose = y is LooseGroupModel;
        if (xLoose || yLoose)
            return xLoose ? 1 : -1;

        var xi = IndexOfSource(x.Source);
        var yi = IndexOfSource(y.Source);
        if (xi >= 0 && yi >= 0)
            return xi.CompareTo(yi);
        if (xi >= 0)
            return -1;
        if (yi >= 0)
            return 1;

        var tier = TierOf(x).CompareTo(TierOf(y));
        return tier != 0 ? tier : string.CompareOrdinal(x.Source, y.Source);
    }

    private int IndexOfSource(string? source)
    {
        if (source is null)
            return -1;
        for (var i = 0; i < sourceOrders.Count; i++)
            if (sourceOrders[i] == source)
                return i;
        return -1;
    }

    // 不在 SourceOrders 里的组：Recipe 排在 Modpack 前（POLY-116 默认档位）
    private static int TierOf(GroupModelBase g) =>
        g.Kind == PackageSourceHelper.Kind.Recipe ? 0 : 1;

    private static int RankOf(PackageListItemBase item) => item is PackageListItemBase.Header ? 0 : 1;

    private static int IntraIndexOf(PackageListItemBase item) =>
        item is PackageListItemBase.Entry e ? e.Package.PersistentIndex : 0;
}
