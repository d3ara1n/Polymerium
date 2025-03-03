using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Polymerium.App.Assets;

public static class AssetUriIndex
{
    public const string REPOSITORY_HEADER_CURSEFORGE =
        "avares://Polymerium.App/Assets/Images/Repositories/Header_CurseForge.png";

    public const string REPOSITORY_HEADER_MODRINTH =
        "avares://Polymerium.App/Assets/Images/Repositories/Header_MODRINTH.png";

    public static readonly Uri DIRT_IMAGE = new("avares://Polymerium.App/Assets/Images/Placeholders/Dirt.png",
                                                UriKind.Absolute);

    private static readonly Uri WALLPAPER_IMAGE = new("avares://Polymerium.App/Assets/Images/Wallpaper.png",
                                                      UriKind.Absolute);

    public static readonly Bitmap DIRT_IMAGE_BITMAP = new(AssetLoader.Open(DIRT_IMAGE));

    public static readonly Bitmap WALLPAPER_IMAGE_BITMAP = new(AssetLoader.Open(WALLPAPER_IMAGE));
}