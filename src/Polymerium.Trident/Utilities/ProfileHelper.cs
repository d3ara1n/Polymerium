namespace Polymerium.Trident.Utilities;

public static class ProfileHelper
{
    public static string? PickIcon(string key)
    {
        return FileHelper.PickExists(
            PathDef.Default.DirectoryOfHome(key), ["icon.png", "icon.jpeg", "icon.jpg", "icon.webp", "icon.bmp"]);
    }

    public static string? PickScreenshotRandomly(string key)
    {
        return FileHelper.PickRandomly(Path.Combine(PathDef.Default.DirectoryOfBuild(key), "screenshots"), "*.png");
    }
}