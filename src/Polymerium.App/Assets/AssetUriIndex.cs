using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Polymerium.App.Assets;

public static class AssetUriIndex
{
    private const string DIRT_IMAGE = "avares://Polymerium.App/Assets/Images/Placeholders/Dirt.png";

    public const string WALLPAPER_IMAGE = "avares://Polymerium.App/Assets/Images/Wallpaper.png";

    public const string REPOSITORY_HEADER_CURSEFORGE = "avares://Polymerium.App/Assets/Images/Repositories/Header_CurseForge.png";

    public const string REPOSITORY_HEADER_MODRINTH = "avares://Polymerium.App/Assets/Images/Repositories/Header_MODRINTH.png";

    public static readonly Bitmap DIRT_IMAGE_BITMAP = new(AssetLoader.Open(new Uri(DIRT_IMAGE)));
}