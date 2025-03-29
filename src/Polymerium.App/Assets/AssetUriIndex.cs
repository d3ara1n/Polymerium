using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Polymerium.App.Assets;

public static class AssetUriIndex
{
    public static readonly Uri REPOSITORY_HEADER_CURSEFORGE =
        new("avares://Polymerium.App/Assets/Images/Repositories/Header_CurseForge.png", UriKind.Absolute);

    public static readonly Uri REPOSITORY_HEADER_MODRINTH =
        new("avares://Polymerium.App/Assets/Images/Repositories/Header_Modrinth.png", UriKind.Absolute);

    public static readonly Uri REPOSITORY_HEADER_FAVORITE =
        new("avares://Polymerium.App/Assets/Images/Repositories/Header_Favorite.png", UriKind.Absolute);

    public static readonly Uri DIRT_IMAGE = new("avares://Polymerium.App/Assets/Images/Placeholders/Dirt.png",
                                                UriKind.Absolute);

    private static readonly Uri WALLPAPER_IMAGE = new("avares://Polymerium.App/Assets/Images/Wallpaper.png",
                                                      UriKind.Absolute);

    public static readonly Bitmap REPOSITORY_HEADER_CURSEFORGE_BITMAP =
        new(AssetLoader.Open(REPOSITORY_HEADER_CURSEFORGE));

    public static readonly Bitmap REPOSITORY_HEADER_MODRINTH_BITMAP = new(AssetLoader.Open(REPOSITORY_HEADER_MODRINTH));

    public static readonly Bitmap REPOSITORY_HEADER_FAVORITE_BITMAP = new(AssetLoader.Open(REPOSITORY_HEADER_FAVORITE));

    public static readonly Bitmap DIRT_IMAGE_BITMAP = new(AssetLoader.Open(DIRT_IMAGE));

    public static readonly Bitmap WALLPAPER_IMAGE_BITMAP = new(AssetLoader.Open(WALLPAPER_IMAGE));
}