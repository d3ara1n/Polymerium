using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Models;

public partial class PackDataModel : ModelBase
{
    private readonly PackData _pack;

    /// <inheritdoc/>
    public PackDataModel(PackData pack)
    {
        _pack = pack;
        ExcludedTags = new(_pack.ExcludedTags, x => x, x => x);
        IncludingSource = pack.IncludingSource;
        IncludingTags = pack.IncludingTags;
        JavaMaxMemory = GetEntry(Profile.OVERRIDE_JAVA_MAX_MEMORY);
        JavaAdditionalArguments = GetEntry(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS);
        ConnectServer = GetEntry(Profile.OVERRIDE_BEHAVIOR_CONNECT_SERVER);
    }

    #region Reactive

    [ObservableProperty]
    public partial bool IncludingSource { get; set; }

    partial void OnIncludingSourceChanged(bool value) => _pack.IncludingSource = value;

    [ObservableProperty]
    public partial bool IncludingTags { get; set; }

    partial void OnIncludingTagsChanged(bool value) => _pack.IncludingTags = value;

    [ObservableProperty]
    public partial bool JavaMaxMemory { get; set; }

    partial void OnJavaMaxMemoryChanged(bool value) =>
        SetEntry(Profile.OVERRIDE_JAVA_MAX_MEMORY, value);

    [ObservableProperty]
    public partial bool JavaAdditionalArguments { get; set; }

    partial void OnJavaAdditionalArgumentsChanged(bool value) =>
        SetEntry(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS, value);

    [ObservableProperty]
    public partial bool ConnectServer { get; set; }

    partial void OnConnectServerChanged(bool value) =>
        SetEntry(Profile.OVERRIDE_BEHAVIOR_CONNECT_SERVER, value);

    public MappingCollection<string, string> ExcludedTags { get; }

    #endregion

    #region Other

    private bool GetEntry(string key)
    {
        return _pack.IncludedOverrides.Any(x => x.Key == key && x.Enabled);
    }

    private void SetEntry(string key, bool value)
    {
        var found = _pack.IncludedOverrides.FirstOrDefault(x => x.Key == key);
        if (found != null)
        {
            // 但是设置为不启用并不会额外的去移除
            found.Enabled = value;
        }
        // 只有设置为启用才会额外添加
        else if (value)
        {
            _pack.IncludedOverrides.Add(new() { Key = key, Enabled = value });
        }
    }

    #endregion
}
