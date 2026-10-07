using System;
using Polymerium.Avalonia.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.Models;

public partial class InstanceBasicModel : ModelBase
{
    public InstanceBasicModel(string key, string name, string version, string? loader, string? source)
    {
        Key = key;
        Name = name;
        Version = version;
        Loader = loader;
        Source = source;
        Thumbnail = AssetUriIndex.DirtImage;

        UpdateIcon();
    }

    #region Direct

    public string Key { get; }

    #endregion

    public void UpdateIcon()
    {
        var iconPath = InstanceHelper.PickIcon(Key);
        Thumbnail = iconPath is not null ? ImageSourceHelper.FromFile(iconPath) : AssetUriIndex.DirtImage;
    }

    #region Reactive

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Version { get; set; }

    [ObservableProperty]
    public partial string LoaderLabel { get; set; } = "Enum_Vanilla";

    [ObservableProperty]
    public partial string SourceLabel { get; set; } = "local";

    [ObservableProperty]
    public partial Uri Thumbnail { get; set; }

    [ObservableProperty]
    public partial string? Source { get; set; }

    partial void OnSourceChanged(string? value)
    {
        SourceLabel = ModpackSourceHelper.TryGetPref(value, out var result) ? result.Repository : "local";
    }

    [ObservableProperty]
    public partial string? Loader { get; set; }

    partial void OnLoaderChanged(string? value)
    {
        if (value != null && LoaderHelper.TryParse(value, out var result))
        {
            LoaderLabel = LoaderHelper.ToDisplayName(result.Identity);
        }
        else
        {
            LoaderLabel = "Enum_Vanilla";
        }
    }

    #endregion
}
