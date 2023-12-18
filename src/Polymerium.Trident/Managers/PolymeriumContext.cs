using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Trident.Managers
{
    public class PolymeriumContext(string home)
    {
        public const string DIR_INSTANCE = "instances";
        public const string DIR_STORAGE = "storages";
        public const string DIR_CACHE = "cache";
        public const string DIR_CACHE_LIBRARY = "libraries";
        public const string DIR_CACHE_ASSET = "assets";
        public const string DIR_CACHE_OBJECT = "objects";

        public const string FILE_ENTRY_MANIFEST = "instances.json";

        public string HomeDir => home;
        public string InstanceDir => Path.Combine(HomeDir, DIR_INSTANCE);
        public string StorageDir => Path.Combine(HomeDir, DIR_STORAGE);
        public string CacheDir => Path.Combine(HomeDir, DIR_CACHE);
        public string LibraryDir => Path.Combine(HomeDir, DIR_CACHE_LIBRARY);
        public string AssetDir => Path.Combine(HomeDir, DIR_CACHE_ASSET);
        public string ObjectDir => Path.Combine(HomeDir, DIR_CACHE_OBJECT);

        public string ManifestFile => Path.Combine(HomeDir, FILE_ENTRY_MANIFEST);

    }
}
