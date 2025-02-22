using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.Trident.Utilities;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Models;

public partial class InstanceBasicModel : ModelBase
{
    public InstanceBasicModel(string key, string name, string version, string? loader, string? source)
    {
        Key = key;
        Name = name;
        Version = version;
        Loader = loader;
        Source = source;

        _thumbnail = null!;

        UpdateIcon();
    }

    #region Direct Properties

    public string Key { get; }

    #endregion

    public void UpdateIcon()
    {
        var iconPath = ProfileHelper.PickIcon(Key);
        Thumbnail = iconPath is not null ? new Bitmap(iconPath) : AssetUriIndex.DIRT_IMAGE_BITMAP;
    }

    #region Reactive Properties

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _version;

    [ObservableProperty]
    private string _loaderLabel = "Vanilla";

    [ObservableProperty]
    private string _sourceLabel = "local";

    [ObservableProperty]
    private Bitmap _thumbnail;

    private string? _source;

    public string? Source
    {
        get => _source;
        set
        {
            SetProperty(ref _source, value);
            if (!string.IsNullOrEmpty(value) && PackageHelper.TryParse(value, out var result))
                SourceLabel = result.Label;
        }
    }

    private string? _loader;

    public string? Loader
    {
        get => _loader;
        set
        {
            SetProperty(ref _loader, value);


            if (value != null && LoaderHelper.TryParse(value, out var result))
                // TODO: 从语言文件中选取
                LoaderLabel = result.Identity switch
                {
                    LoaderHelper.LOADERID_FORGE => "Forge",
                    LoaderHelper.LOADERID_NEOFORGE => "NeoForge",
                    LoaderHelper.LOADERID_FABRIC => "Fabric",
                    LoaderHelper.LOADERID_QUILT => "QUILT",
                    LoaderHelper.LOADERID_FLINT => "Flint",
                    _ => result.Identity
                };
            else
                LoaderLabel = "Vanilla";
        }
    }

    #endregion
}