using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Polymerium.Avalonia.Assets;

public static class AssetUriIndex
{
    public static readonly Uri Icon = new(
        "avares://Polymerium/Assets/Icon.png",
        UriKind.Absolute
    );

    public static readonly Uri RepositoryHeaderCurseforge = new(
        "avares://Polymerium/Assets/Images/Repositories/Header_CurseForge.png",
        UriKind.Absolute
    );

    public static readonly Uri RepositoryHeaderModrinth = new(
        "avares://Polymerium/Assets/Images/Repositories/Header_Modrinth.png",
        UriKind.Absolute
    );

    public static readonly Uri RepositoryHeaderFavorite = new(
        "avares://Polymerium/Assets/Images/Repositories/Header_Favorite.png",
        UriKind.Absolute
    );

    public static readonly Uri DirtImage = new(
        "avares://Polymerium/Assets/Images/Placeholders/Dirt.png",
        UriKind.Absolute
    );

    public static readonly Uri SteveFaceImage = new(
        "avares://Polymerium/Assets/Images/Placeholders/Steve_Face.png",
        UriKind.Absolute
    );

    public static readonly Uri WallpaperImage = new(
        "avares://Polymerium/Assets/Images/Wallpaper.png",
        UriKind.Absolute
    );

    public static readonly Uri LoaderNeoforge = new(
        "avares://Polymerium/Assets/Loaders/net.neoforged.png",
        UriKind.Absolute
    );

    public static readonly Uri LoaderForge = new(
        "avares://Polymerium/Assets/Loaders/net.minecraftforge.png",
        UriKind.Absolute
    );

    public static readonly Uri LoaderFabric = new(
        "avares://Polymerium/Assets/Loaders/net.fabricmc.png",
        UriKind.Absolute
    );

    public static readonly Uri LoaderQuilt = new(
        "avares://Polymerium/Assets/Loaders/org.quiltmc.png",
        UriKind.Absolute
    );

    public static readonly Bitmap RepositoryHeaderCurseforgeBitmap = new(
        AssetLoader.Open(RepositoryHeaderCurseforge)
    );

    public static readonly Bitmap RepositoryHeaderModrinthBitmap = new(
        AssetLoader.Open(RepositoryHeaderModrinth)
    );

    public static readonly Bitmap RepositoryHeaderFavoriteBitmap = new(
        AssetLoader.Open(RepositoryHeaderFavorite)
    );

    public static readonly Bitmap DirtImageBitmap = new(AssetLoader.Open(DirtImage));

    public static readonly Bitmap WallpaperImageBitmap = new(AssetLoader.Open(WallpaperImage));

    public static readonly Bitmap IconBitmap = new(AssetLoader.Open(Icon));

    public static readonly Bitmap LoaderNeoforgeBitmap = new(AssetLoader.Open(LoaderNeoforge));
    public static readonly Bitmap LoaderForgeBitmap = new(AssetLoader.Open(LoaderForge));
    public static readonly Bitmap LoaderFabricBitmap = new(AssetLoader.Open(LoaderFabric));
    public static readonly Bitmap LoaderQuiltBitmap = new(AssetLoader.Open(LoaderQuilt));
}
