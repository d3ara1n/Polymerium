using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.FileModels;

namespace Polymerium.Avalonia.Models;

public partial class InstancePackageModel(Profile.Rice.Entry entry, bool canUpdate) : ModelBase
{
    #region Other

    /// <summary>
    ///     这是个 Hacky 解决方案，用于判断 Entry 是否被 InstancePackageModel 无法观测的地方被修改过
    ///     例如当（未来新增的）版本更新等修改了 Entry.Purl 中的版本信息
    ///     此时 InstanceSetupPage 需要刷新，就会判断 Entry.Purl 和 OldPurl
    ///     如果是 InstancePackageModel 中的修改（例如由 InstanceSetupPage 发起）是会同时修改两边的值避免触发刷新
    /// </summary>
    public string OldPurlCache { get; set; } = entry.Purl;

    #endregion

    #region Direct

    public Profile.Rice.Entry Entry => entry;

    public bool CanRemove => entry.Source is null;

    public int PersistentIndex { get; set; }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool CanUpdate { get; set; } = canUpdate;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = entry.Enabled;

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    partial void OnIsEnabledChanged(bool value) => entry.Enabled = value;

    [ObservableProperty]
    public partial InstancePackageInfoModel? Info { get; set; }

    public MappingCollection<string, string> Tags { get; } = new(entry.Tags, x => x, x => x);

    #endregion
}
