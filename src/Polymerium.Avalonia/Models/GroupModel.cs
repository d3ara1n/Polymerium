using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.Models;

// 包归组：有源组（Modpack / Recipe，Source 非空）直接用本类型实例；散装组用 LooseGroupModel 特化。
// 类型相关的展示数据落在 Info（多态），组本身只承载类型无关的壳——展开态、计数、加载态、按钮与右键菜单。
public partial class GroupModel : ModelBase
{
    public required PackageSourceHelper.Kind Kind { get; init; }

    public required string? Source { get; init; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;

    [ObservableProperty]
    public partial int Count { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    public partial GroupInfoModelBase? Info { get; set; }

    public virtual bool RequireGuideLine => true;
}
