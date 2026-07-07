using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.Avalonia.Models;

// 整合包组：既是分组依据，又持有从 Merge-Load 阶段解析过来的 Modpack 信息（Name/Thumbnail）。
// 解析路径同 InstanceSetupPage 顶部 Reference（PackageHelper.TryParse + DataService.ResolvePackageAsync）。
public sealed partial class ModpackGroupModel : GroupModelBase
{
    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial Uri? Thumbnail { get; set; }

    // Merge-Load 阶段尝试解析后置 true，避免重复加载（成功失败都置位）
    public bool IsInfoLoaded { get; set; }
}
