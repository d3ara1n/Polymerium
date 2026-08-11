using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.FileModels;

namespace Polymerium.Avalonia.Models;

public partial class PackDataModel : ModelBase
{
    private readonly PackData _pack;

    /// <inheritdoc />
    public PackDataModel(PackData pack)
    {
        _pack = pack;
        ExcludedTags = [with(_pack.ExcludedTags, x => x, x => x)];
        OfflineMode = pack.OfflineMode;
        IncludingSource = pack.IncludingSource;
        IncludingTags = pack.IncludingTags;
        JavaMaxMemory = GetEntry(Profile.OVERRIDE_JAVA_MAX_MEMORY);
        JavaAdditionalArguments = GetEntry(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS);
        ConnectServer = GetEntry(Profile.OVERRIDE_BEHAVIOR_CONNECT_SERVER);
    }

    #region Reactive

    [ObservableProperty]
    public partial bool OfflineMode { get; set; }

    partial void OnOfflineModeChanged(bool value) => _pack.OfflineMode = value;

    [ObservableProperty]
    public partial bool IncludingSource { get; set; }

    partial void OnIncludingSourceChanged(bool value) => _pack.IncludingSource = value;

    [ObservableProperty]
    public partial bool IncludingTags { get; set; }

    partial void OnIncludingTagsChanged(bool value) => _pack.IncludingTags = value;

    [ObservableProperty]
    public partial bool JavaMaxMemory { get; set; }

    partial void OnJavaMaxMemoryChanged(bool value) => SetEntry(Profile.OVERRIDE_JAVA_MAX_MEMORY, value);

    [ObservableProperty]
    public partial bool JavaAdditionalArguments { get; set; }

    partial void OnJavaAdditionalArgumentsChanged(bool value) =>
        SetEntry(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS, value);

    [ObservableProperty]
    public partial bool ConnectServer { get; set; }

    partial void OnConnectServerChanged(bool value) => SetEntry(Profile.OVERRIDE_BEHAVIOR_CONNECT_SERVER, value);

    public MappingCollection<string, string> ExcludedTags { get; }

    #endregion

    #region Other

    private bool GetEntry(string key) => _pack.IncludedOverrides.Any(x => x.Key == key && x.Enabled);

    private void SetEntry(string key, bool value)
    {
        var found = _pack.IncludedOverrides.FirstOrDefault(x => x.Key == key);
        if (found != null)
        {
            // NOTE: 禁用只置 Enabled=false 不移除条目，启用时才新增条目。
            found.Enabled = value;
        }
        else if (value)
        {
            _pack.IncludedOverrides.Add(new() { Key = key, Enabled = value });
        }
    }

    #endregion
}
