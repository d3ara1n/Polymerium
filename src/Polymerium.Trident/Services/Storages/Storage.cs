namespace Polymerium.Trident.Services.Storages
{
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
            string[] dirs = Directory.GetDirectories(_path);
            string[] files = Directory.GetFiles(_path);
            foreach (string dir in dirs)
            {
                Directory.Delete(dir, true);
            }

            foreach (string file in files)
            {
                File.Delete(file);
            }
        }

        public async Task WriteAsync(string fileName, byte[] content)
        {
            string path = Path.Combine(_path, fileName);
            string? dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                await File.WriteAllBytesAsync(path, content);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}