using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

// 多态列表项：Header 渲染组，Entry 渲染包。两者都持有同一个 GroupModelBase 实例——
// 这个 Group 既是分组依据（同组共享实例），又是组信息的载体。
public abstract class PackageListItemBase : ModelBase
{
    public required PackageListKey Key { get; init; }

    public required GroupModelBase Group { get; init; }

    public sealed class Header : PackageListItemBase;

    public sealed class Entry : PackageListItemBase
    {
        public required InstancePackageModel Package { get; init; }
    }
}
