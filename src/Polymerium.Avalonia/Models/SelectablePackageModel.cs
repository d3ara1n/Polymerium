using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using System.Collections.Generic;
using System.Linq;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Models;

// PackageSelectorDialog 的行模型：包装 InstancePackageModel，加上选择状态与去范式化的显示/圈选字段。
// Info 为 null（加载失败的包）也能正常显示与选中——删除加载失败的包正是常见诉求。
// Key 由调用方（持有 _flat 的页面）在构建候选时带入，删除时原样取用，不在消费侧重建。
public partial class SelectablePackageModel(InstancePackageModel source, PackageListKey key) : ModelBase
{
    public InstancePackageModel Source { get; } = source;

    public PackageListKey Key { get; } = key;

    // 去范式化：dialog 不依赖 Info 是否加载完成，Label 回退到 Pref 保证失败包可辨识
    public string Label { get; } = source.Info?.ProjectName ?? source.Entry.Pref;

    public string? Author { get; } = source.Info?.Author;

    public ResourceKind? Kind { get; } = source.Info?.Kind;

    public Bitmap? Thumbnail { get; } = source.Info?.Thumbnail;

    public IReadOnlyList<string> Tags { get; } = source.Tags.ToList();

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
