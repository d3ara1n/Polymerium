namespace Polymerium.Trident.Services;

public class TridentContext(string home)
{
    public const string DIR_INSTANCE = "instances";
    public const string DIR_STORAGE = "storages";
    public const string DIR_CACHE = "cache";
    public const string DIR_CACHE_LIBRARY = "libraries";
    public const string DIR_CACHE_ASSET = "assets";
    public const string DIR_CACHE_OBJECT = "objects";
    public const string DIR_CACHE_METADATA = "metadata";
    public const string DIR_PRIVATE = ".polymerium";
    public const string DIR_PRIVATE_LOG = "logs";
    public const string DIR_PRIVATE_BACKUP = "backups";
    public const string DIR_PRIVATE_THUMBNAIL = "thumbnails";

    public const string FILE_ACCOUNT_VAULT = "account.vault.json";
    public const string FILE_FAVORITE_LIST = "favorites.json";

    public string HomeDir => home;
    public string InstanceDir => Path.Combine(HomeDir, DIR_INSTANCE);
    public string StorageDir => Path.Combine(HomeDir, DIR_STORAGE);
    public string CacheDir => Path.Combine(HomeDir, DIR_CACHE);
    public string LibraryDir => Path.Combine(HomeDir, DIR_CACHE, DIR_CACHE_LIBRARY);
    public string AssetDir => Path.Combine(HomeDir, DIR_CACHE, DIR_CACHE_ASSET);
    public string ObjectDir => Path.Combine(HomeDir, DIR_CACHE, DIR_CACHE_OBJECT);
    public string MetadataDir => Path.Combine(HomeDir, DIR_CACHE, DIR_CACHE_METADATA);
    public string AccountVaultFile => Path.Combine(HomeDir, DIR_PRIVATE, FILE_ACCOUNT_VAULT);
    public string FavoriteListFile => Path.Combine(HomeDir, DIR_PRIVATE, FILE_FAVORITE_LIST);
    public string LogDir => Path.Combine(HomeDir, DIR_PRIVATE, DIR_PRIVATE_LOG);
    public string BackupDir => Path.Combine(HomeDir, DIR_PRIVATE, DIR_PRIVATE_BACKUP);
    public string ThumbnailDir => Path.Combine(HomeDir, DIR_PRIVATE, DIR_PRIVATE_THUMBNAIL);

    public string InstanceHomePath(string key)
    {
        return Path.Combine(InstanceDir, key);
    }

    public string InstanceArtifactPath(string key)
    {
        return Path.Combine(InstanceDir, key, "trident.artifact.json");
    }
}