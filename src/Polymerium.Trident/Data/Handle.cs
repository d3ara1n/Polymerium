using System.Text.Json;

namespace Polymerium.Trident.Data
{
    public class Handle<T>(T instance, string path, JsonSerializerOptions? options = null)
        : IDisposable
        where T : class
    {
        private readonly JsonSerializerOptions _options = options ?? JsonSerializerOptions.Default;

        public T Value { get; } = instance;
        public string Path { get; } = path;
        public bool Activated { get; set; } = true;

        public void Dispose()
        {
            Flush();
            GC.SuppressFinalize(this);
        }

        public static Handle<T>? Create(string path, JsonSerializerOptions? options = null)
        {
            if (File.Exists(path))
            {
                try
                {
                    var content = File.ReadAllText(path);
                    var inst = JsonSerializer.Deserialize<T>(content, options);
                    if (inst != default)
                    {
                        return new Handle<T>(inst, path, options);
                    }
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public void Flush()
        {
            if (Activated)
            {
                var parent = System.IO.Path.GetDirectoryName(Path);
                if (parent != null && !Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                var content = JsonSerializer.Serialize(Value, _options);
                File.WriteAllText(Path, content);
            }
        }
    }
}