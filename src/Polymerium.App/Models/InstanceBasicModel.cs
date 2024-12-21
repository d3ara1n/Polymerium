using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.Trident.Utilities;

namespace Polymerium.App.Models;

public partial class InstanceBasicModel : ModelBase
{
    #region Reactive Properties

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _version;
    [ObservableProperty] private string? _loader;
    [ObservableProperty] private string _sourceLabel;
    [ObservableProperty] private Bitmap _thumbnail;
    [ObservableProperty] private InstanceEntryState _state = InstanceEntryState.Idle;

    private string? _source;

    public string? Source
    {
        get => _source;
        set
        {
            if (SetProperty(ref _source, value) && !string.IsNullOrEmpty(value) &&
                PurlHelper.TryParse(value, out var result))
            {
                SourceLabel = result.Label;
            }
            else
            {
                SourceLabel = "local";
            }
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