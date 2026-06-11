using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Properties;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.Models;

public partial class InstanceBasicModel : ModelBase
{
    public InstanceBasicModel(
        string key,
        string name,
        string version,
        string? loader,
        string? source
    )
    {
        Key = key;
        Name = name;
        Version = version;
        Loader = loader;
        Source = source;
        Thumbnail = AssetUriIndex.DirtImageBitmap;

        UpdateIcon();
    }

    #region Direct

    public string Key { get; }

    #endregion

    public void UpdateIcon()
    {
        var iconPath = InstanceHelper.PickIcon(Key);
        Thumbnail = iconPath is not null ? new(iconPath) : AssetUriIndex.DirtImageBitmap;
    }

    #region Reactive

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Version { get; set; }

    [ObservableProperty]
    public partial string LoaderLabel { get; set; } = Resources.Enum_Vanilla;

    [ObservableProperty]
    public partial string SourceLabel { get; set; } = "local";

    [ObservableProperty]
    public partial Bitmap Thumbnail { get; set; }

    [ObservableProperty]
    public partial string? Source { get; set; }

    partial void OnSourceChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value) && PackageHelper.TryParse(value, out var result))
        {
            SourceLabel = result.Label;
        }
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
            LoaderLabel = Resources.Enum_Vanilla;
        }
    }

    #endregion
}
