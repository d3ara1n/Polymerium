using System.Collections.Generic;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Models;

// NOTE: Info 为 null（加载失败）的包也能正常显示与选中——删除加载失败的包正是常见诉求；
//  Key 由调用方在构建候选时带入，删除时原样取用，不在消费侧重建。
public partial class SelectablePackageModel(InstancePackageModel source, PackageListKey key) : ModelBase
{
    public InstancePackageModel Source { get; } = source;

    public PackageListKey Key { get; } = key;

    // NOTE: 去范式化——Label 回退到 Pref，保证加载失败的包仍可辨识。
    public string Label { get; } = source.Info?.ProjectName ?? source.Entry.Pref;

    public string? Author { get; } = source.Info?.Author;

    public ResourceKind? Kind { get; } = source.Info?.Kind;

    public Bitmap? Thumbnail { get; } = source.Info?.Thumbnail;

    public IReadOnlyList<string> Tags { get; } = [.. source.Tags];

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
