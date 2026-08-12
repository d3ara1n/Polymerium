using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.Models;

// NOTE: 有源组（Source 非空）用本类型，散装组用 LooseGroupModel 特化；展示数据落 Info（多态），
//  组本身只承载类型无关的壳（展开态、计数、加载态、按钮）。
public partial class GroupModel : ModelBase
{
    public required PackageSourceHelper.Kind Kind { get; init; }

    public bool IsCollection => Kind == PackageSourceHelper.Kind.Collection;

    public bool IsRecipe => Kind == PackageSourceHelper.Kind.Recipe;

    public required string? Source { get; set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = false;

    [ObservableProperty]
    public partial int Count { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    public partial GroupInfoModelBase? Info { get; set; }

    public virtual bool RequireGuideLine => true;
}
