using System;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Models;

public partial class DesktopItemModel(
    string key,
    string name,
    string version,
    string? loader,
    string? source,
    string? iconPath)
    : ModelBase
{
    #region Reactive Properties

    [ObservableProperty] private string _name = name;

    #endregion

    #region Exposure Properties

    public string Key => key;

    public string Source => source ?? "local";

    public Bitmap Icon { get; } = File.Exists(iconPath)
        ? new Bitmap(iconPath)
        : new Bitmap(AssetLoader.Open(new Uri("avares://Assets/Images/Placeholders/dirt.png")));

    #endregion

    public static DesktopItemModel From(string key, Profile profile, string? iconPath)
    {
        return new DesktopItemModel(key, profile.Name, profile.Setup.Version, profile.Setup.Loader,
            profile.Setup.Source, iconPath);
    }

    public void Update(Profile profile)
    {
        Name = profile.Name;
    }
}