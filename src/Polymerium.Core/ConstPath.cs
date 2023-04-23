namespace Polymerium.Core;

#pragma warning disable S1075 // URIs should not be hardcoded
public static class ConstPath
{
    public const string INSTANCE_BASE = "poly-file://{0}/";
    public const string INSTANCE_NATIVES_DIR = "poly-file://{0}/natives/";
    public const string INSTANCE_POLYLOCKDATA_FILE = "poly-file://{0}/polymerium.lock.json";
    public const string INSTANCE_POLYLOCKHASH_FILE = "poly-file://{0}/polymerium.lock.json.hash";
    public const string CACHE_BASE = "poly-file:///cache/";
    public const string CACHE_OBJECTS_DIR = "poly-file:///cache/objects/";
    public const string CACHE_OBJECTS_FILE = "poly-file:///cache/objects/{0}.obj";
    public const string CACHE_ASSETS_DIR = "poly-file:///cache/assets/";
    public const string CACHE_ASSETS_INDEX_FILE = "poly-file:///cache/assets/indexes/{0}.json";
    public const string CACHE_ASSETS_OBJECTS_FILE = "poly-file:///cache/assets/objects/{0}/{1}";
    public const string CACHE_LIBRARIES_DIR = "poly-file:///cache/libraries/";
    public const string CONFIG_ACCOUNT_FILE = "poly-file:///accounts.json";
    public const string CONFIG_CONFIGURATION_FILE = "poly-file:///configuration.json";
    public const string CONFIG_INSTANCE_FILE = "poly-file:///instances.json";
    public const string LOCAL_INSTANCE_BASE = "poly-file:///local/instances/{0}/";
    public const string LOCAL_INSTANCE_LIBRARIES_DIR =
        "poly-file:///local/instances/{0}/libraries/";
    public const string LOCAL_MODPACK_BASE = "poly-file:///local/modpack/{0}/{1}/";
}
#pragma warning restore S1075 // URIs should not be hardcoded
