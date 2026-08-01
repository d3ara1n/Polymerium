using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.Avalonia.Models;

// Recipe 组：Source == recipe://<id> 的包归组。Name/IsMissing 来自持久层 recipe 摘要；
// recipe 被删除后 Source 仍残留在 profile 中，IsMissing 置位，组头降级显示 uri 原文。
public sealed partial class RecipeGroupModel : GroupModelBase
{
    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial bool IsMissing { get; set; }
}
