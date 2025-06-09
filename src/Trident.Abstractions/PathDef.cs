using System.Diagnostics;

namespace Trident.Abstractions;

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

    #region Private Folder

    public string PrivateDirectory(string brand) => Path.Combine(Home, $".{brand.ToLower()}");

    // public string FileOfPrivateSettings(string brand) => Path.Combine(PrivateDirectory(brand), "settings.json");
    // public string FileOfPrivateCache(string brand) => Path.Combine(PrivateDirectory(brand), "cache.sqlite.db");
    // public string FileOfPrivatePersistence(string brand) => Path.Combine(PrivateDirectory(brand), "persistence.sqlite.db");

    #endregion

    #region Cache Folder

    public string CacheDirectory => Path.Combine(Home, "cache");

    public string CacheAssetDirectory => Path.Combine(CacheDirectory, "assets");
    public string CacheLibraryDirectory => Path.Combine(CacheDirectory, "libraries");
    public string CachePackageDirectory => Path.Combine(CacheDirectory, "packages");
    public string CacheRuntimeDirectory => Path.Combine(CacheDirectory, "runtimes");

    public string FileOfRuntimeBundle(uint major) => Path.Combine(CacheRuntimeDirectory, $"{major}.zip");

    public string DirectoryOfRuntime(uint major) => Path.Combine(CacheRuntimeDirectory, major.ToString());

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

    public string FileOfPackageObject(string label, string? ns, string pid, string vid, string extension) =>
        ns != null
            ? Path.Combine(CachePackageDirectory, label, ns, pid, $"{vid}{extension}")
            : Path.Combine(CachePackageDirectory, label, pid, $"{vid}{extension}");

    public string FileOfPackageMetadata(string label, string? ns, string pid, string vid) =>
        ns != null
            ? Path.Combine(CachePackageDirectory, label, ns, pid, $"{vid}.json")
            : Path.Combine(CachePackageDirectory, label, pid, $"{vid}.json");

    public string FileOfAssetIndex(string index) => Path.Combine(CacheAssetDirectory, "indexes", $"{index}.json");

    public string FileOfAssetObject(string hash) => Path.Combine(CacheAssetDirectory, "objects", hash[..2], hash);

    #endregion

    #region Instance Folder

    public string InstanceDirectory => Path.Combine(Home, "instances");

    public string FileOfProfile(string key) => Path.Combine(InstanceDirectory, key, "profile.json");

    public string FileOfPreference(string key) => Path.Combine(InstanceDirectory, key, "preference.json");

    public string FileOfIcon(string key, string extensionGuess) =>
        Path.Combine(InstanceDirectory, key, $"icon.{extensionGuess}");

    public string FileOfLockData(string key) => Path.Combine(InstanceDirectory, key, "data.lock.json");

    public string FileOfUserData(string key) => Path.Combine(InstanceDirectory, key, "data.user.json");

    public string FileOfBomb(string key) => Path.Combine(InstanceDirectory, key, "_bomb_has_been_planted_");

    public string DirectoryOfHome(string key) => Path.Combine(InstanceDirectory, key);

    public string DirectoryOfBuild(string key) => Path.Combine(InstanceDirectory, key, "build");
    public string DirectoryOfNatives(string key) => Path.Combine(DirectoryOfBuild(key), "natives");

    public string DirectoryOfImport(string key) => Path.Combine(InstanceDirectory, key, "import");

    public string DirectoryOfPersist(string key) => Path.Combine(InstanceDirectory, key, "persist");

    public string DirectoryOfSnapshots(string key) => Path.Combine(InstanceDirectory, key, "snapshots");

    #endregion
}