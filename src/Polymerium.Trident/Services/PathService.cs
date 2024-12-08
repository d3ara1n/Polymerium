namespace Polymerium.Trident.Services;

public class PathService
{
    public PathService()
    {
        var dir = Directory.GetCurrentDirectory();
        string? home = null;
        while (dir is not null && Directory.Exists(dir))
        {
            var target = Path.Combine(dir, ".trident");
            if (Directory.Exists(home))
            {
                home = target;
                break;
            }

            dir = Path.GetDirectoryName(dir);
        }

        Home = home ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".trident");
    }


    public string Home { get; }

    #region Cache Folder

    public string CacheDirectory => Path.Combine(Home, "cache");

    public string CacheAssetDirectory => Path.Combine(CacheDirectory, "assets");
    public string CacheLibraryDirectory => Path.Combine(CacheDirectory, "libraries");
    public string CachePackageDirectory => Path.Combine(CacheDirectory, "packages");
    public string CacheObjectDirectory => Path.Combine(CacheDirectory, "objects");

    #endregion

    #region Instance Folder

    public string InstanceDirectory => Path.Combine(Home, "instances");

    public string FileOfProfile(string key)
    {
        return Path.Combine(InstanceDirectory, key, "profile.toml");
    }

    public string FileOfPreference(string key)
    {
        return Path.Combine(InstanceDirectory, key, "preference.toml");
    }

    public string FileOfIcon(string key)
    {
        return Path.Combine(InstanceDirectory, key, "icon.png");
    }

    public string FileOfLockData(string key)
    {
        return Path.Combine(InstanceDirectory, key, "data.lock.json");
    }

    public string FileOfUserData(string key)
    {
        return Path.Combine(InstanceDirectory, key, "data.user.json");
    }

    public string DirectoryOfBuild(string key)
    {
        return Path.Combine(InstanceDirectory, key, "build");
    }

    public string DirectoryOfImport(string key)
    {
        return Path.Combine(InstanceDirectory, key, "import");
    }

    public string DirectoryOfPersist(string key)
    {
        return Path.Combine(InstanceDirectory, key, "persist");
    }

    public string DirectoryOfSnapshots(string key)
    {
        return Path.Combine(InstanceDirectory, key, "snapshots");
    }

    #endregion
}