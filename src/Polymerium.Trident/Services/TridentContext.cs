namespace Polymerium.Trident.Services
{
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

        public const string FILE_ARTIFACT = "trident.artifact.json";
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

        public string LibraryPath(string ns, string name, string version, string? platform, string extension)
        {
            var nsDir = string.Join(Path.DirectorySeparatorChar, ns.Split('.'));
            return Path.Combine(LibraryDir, nsDir, name, version,
                platform != null ? $"{name}-{version}-{platform}.{extension}" : $"{name}-{version}.{extension}");
        }

        public string InstanceArtifactPath(string key)
        {
            return Path.Combine(InstanceDir, key, FILE_ARTIFACT);
        }

        public string InstanceProfilePath(string key)
        {
            return Path.Combine(InstanceDir, $"{key}.json");
        }

        public string AssetIndexPath(string index)
        {
            return Path.Combine(AssetDir, "indexes", $"{index}.json");
        }

        public string AssetObjectPath(string hash)
        {
            return Path.Combine(AssetDir, "objects", hash[..2], hash);
        }

        public string NativeDirPath(string key)
        {
            return Path.Combine(InstanceHomePath(key), "natives");
        }
    }
}