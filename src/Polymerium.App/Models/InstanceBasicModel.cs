using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.Trident.Utilities;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Models;

public partial class InstanceBasicModel : ModelBase
{
    #region Reactive Properties

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _version;
    [ObservableProperty] private string _loaderLabel = "Vanilla";
    [ObservableProperty] private string _sourceLabel = "local";
    [ObservableProperty] private Bitmap _thumbnail;

    private string? _source;

    public string? Source
    {
        get => _source;
        set
        {
            SetProperty(ref _source, value);
            if (!string.IsNullOrEmpty(value) &&
                PackageHelper.TryParse(value, out var result))
            {
                SourceLabel = result.Label;
            }
        }
    }

    private string? _loader;

    public string? Loader
    {
        get => _loader;
        set
        {
            SetProperty(ref _loader, value);
            LoaderLabel = "Vanilla";
        }
    }

    #endregion

    #region Direct Properties

    public string Key { get; }

    #endregion

    public InstanceBasicModel(string key, string name, string version, string? loader, string? source)
    {
        Key = key;
        Name = name;
        Version = version;
        Loader = loader;
        Source = source;


        var iconPath = ProfileHelper.PickIcon(key);
        Thumbnail = iconPath is not null
            ? new Bitmap(iconPath)
            : new Bitmap(AssetLoader.Open(new Uri(AssetUriIndex.DIRT_IMAGE)));
    }
}