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

    public static readonly Uri STEVE_FACE_IMAGE =
        new("avares://Polymerium.App/Assets/Images/Placeholders/Steve_Face.png", UriKind.Absolute);

    private static readonly Uri WALLPAPER_IMAGE = new("avares://Polymerium.App/Assets/Images/Wallpaper.png",
                                                      UriKind.Absolute);

    private static readonly Uri LOADER_NEOFORGE = new("avares://Polymerium.App/Assets/Loaders/net.neoforged.png",
                                                      UriKind.Absolute);

    private static readonly Uri LOADER_FORGE = new("avares://Polymerium.App/Assets/Loaders/net.minecraftforge.png",
                                                   UriKind.Absolute);

    private static readonly Uri LOADER_FABRIC = new("avares://Polymerium.App/Assets/Loaders/net.fabricmc.png",
                                                    UriKind.Absolute);

    private static readonly Uri LOADER_QUILT = new("avares://Polymerium.App/Assets/Loaders/org.quiltmc.png",
                                                   UriKind.Absolute);

    public static readonly Bitmap REPOSITORY_HEADER_CURSEFORGE_BITMAP =
        new(AssetLoader.Open(REPOSITORY_HEADER_CURSEFORGE));

    public static readonly Bitmap REPOSITORY_HEADER_MODRINTH_BITMAP =
        new(AssetLoader.Open(REPOSITORY_HEADER_MODRINTH));

    public static readonly Bitmap REPOSITORY_HEADER_FAVORITE_BITMAP =
        new(AssetLoader.Open(REPOSITORY_HEADER_FAVORITE));

    public static readonly Bitmap DIRT_IMAGE_BITMAP = new(AssetLoader.Open(DIRT_IMAGE));

    public static readonly Bitmap WALLPAPER_IMAGE_BITMAP = new(AssetLoader.Open(WALLPAPER_IMAGE));

    public static readonly Bitmap LOADER_NEOFORGE_BITMAP = new(AssetLoader.Open(LOADER_NEOFORGE));
    public static readonly Bitmap LOADER_FORGE_BITMAP = new(AssetLoader.Open(LOADER_FORGE));
    public static readonly Bitmap LOADER_FABRIC_BITMAP = new(AssetLoader.Open(LOADER_FABRIC));
    public static readonly Bitmap LOADER_QUILT_BITMAP = new(AssetLoader.Open(LOADER_QUILT));
}
