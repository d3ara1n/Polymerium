using System.Diagnostics;

namespace Polymerium.Trident;

public class PathDef
{
    private PathDef()
    {
        var dir = Directory.GetCurrentDirectory();
        string? home = null;
        while (dir is not null && Directory.Exists(dir))
        {
            var target = Path.Combine(dir, ".trident");
            if (Directory.Exists(target))
            {
                home = target;
                break;
            }

            dir = Path.GetDirectoryName(dir);
        }

        Home = home ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".trident");
        Debug.WriteLine($"Chosen home = {Home}");
    }

    public static PathDef Default { get; } = new();


    public string Home { get; }

    #region Cache Folder

    public string CacheDirectory => Path.Combine(Home, "cache");

    public string CacheAssetDirectory => Path.Combine(CacheDirectory, "assets");
    public string CacheLibraryDirectory => Path.Combine(CacheDirectory, "libraries");
    public string CachePackageDirectory => Path.Combine(CacheDirectory, "packages");
    public string CacheObjectDirectory => Path.Combine(CacheDirectory, "objects");

    public string FileOfLibrary(string ns, string name, string version, string? platform, string extension)
    {
        var nsDir = string.Join(Path.DirectorySeparatorChar, ns.Split('.'));
        return Path.Combine(CacheLibraryDirectory,
                            nsDir,
                            name,
                            version,
                            platform != null
                                ? $"{name}-{version}-{platform}.{extension}"
                                : $"{name}-{version}.{extension}");
    }

    public string FileOfObject(string hash)
    {
        return Path.Combine(CacheObjectDirectory, hash[..2], hash);
    }

    #endregion

    #region Instance Folder

    public string InstanceDirectory => Path.Combine(Home, "instances");

    public string FileOfProfile(string key) => Path.Combine(InstanceDirectory, key, "profile.json");

    public string FileOfPreference(string key) => Path.Combine(InstanceDirectory, key, "preference.json");

    public string FileOfIcon(string key, string extensionGuess) =>
        Path.Combine(InstanceDirectory, key, $"icon.{extensionGuess}");

    public string FileOfLockData(string key) => Path.Combine(InstanceDirectory, key, "data.lock.json");

    public string FileOfUserData(string key) => Path.Combine(InstanceDirectory, key, "data.user.json");

    public string DirectoryOfHome(string key) => Path.Combine(InstanceDirectory, key);

    public string DirectoryOfBuild(string key) => Path.Combine(InstanceDirectory, key, "build");

    public string DirectoryOfImport(string key) => Path.Combine(InstanceDirectory, key, "import");

    public string DirectoryOfPersist(string key) => Path.Combine(InstanceDirectory, key, "persist");

    public string DirectoryOfSnapshots(string key) => Path.Combine(InstanceDirectory, key, "snapshots");

    #endregion
}