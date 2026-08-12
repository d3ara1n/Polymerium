using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

// NOTE: Header 渲染组、Entry 渲染包，两者持同一 GroupModel 实例——它既是分组依据（同组共享），又是组信息载体。
public abstract partial class PackageListItemBase : ModelBase
{
    public required PackageListKey Key { get; init; }

    [ObservableProperty]
    public required partial GroupModel Group { get; set; }

    public sealed class Header : PackageListItemBase;

    public sealed class Entry : PackageListItemBase
    {
        public required InstancePackageModel Package { get; init; }
    }
}
