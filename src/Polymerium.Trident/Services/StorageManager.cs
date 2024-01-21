using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services.Storages;

namespace Polymerium.Trident.Services;

public class StorageManager(PolymeriumContext context)
{
    public Storage Open(string key)
    {
        var path = PathOf(key);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return new Storage(key, path);
    }

    public bool Destroy(string key)
    {
        var path = PathOf(key);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            return true;
        }

        return false;
    }

    public string RequestKey(string key)
    {
        var output = FileNameHelper.Sanitize(key);
        while (Directory.Exists(PathOf(output))) key += '_';
        return output;
    }

    private string PathOf(string key)
    {
        return Path.Combine(context.StorageDir, key);
    }
}