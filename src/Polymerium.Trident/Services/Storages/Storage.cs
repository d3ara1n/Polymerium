namespace Polymerium.Trident.Services.Storages;

public class Storage
{
    private readonly string _path;

    internal Storage(string key, string path)
    {
        Key = key;
        _path = path;
    }

    public string Key { get; }

    public void EnsureEmpty()
    {
        var dirs = Directory.GetDirectories(_path);
        var files = Directory.GetFiles(_path);
        foreach (var dir in dirs)
            Directory.Delete(dir, true);
        foreach (var file in files)
            File.Delete(file);
    }

    public async Task WriteAsync(string fileName, byte[] content)
    {
        var path = Path.Combine(_path, fileName);
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllBytesAsync(path, content);
        }catch(Exception e)
        {
            Console.WriteLine(e);
        }
    }
}