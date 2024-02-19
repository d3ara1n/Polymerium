using System.Text.Json;

namespace Polymerium.Trident.Data
{
    public class Handle<T> : IDisposable
        where T : class
    {
        private readonly JsonSerializerOptions _options;
        private bool disposedValue;

        public Handle(T instance, string path, JsonSerializerOptions? options = null)
        {
            _options = options ?? JsonSerializerOptions.Default;
            Value = instance;
            Path = path;
        }

        public T Value { get; }
        public string Path { get; }
        public bool Activated { get; set; } = true;

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public static Handle<T>? Create(string path, JsonSerializerOptions? options = null)
        {
            if (File.Exists(path))
            {
                try
                {
                    string content = File.ReadAllText(path);
                    T? inst = JsonSerializer.Deserialize<T>(content, options);
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
                string? parent = System.IO.Path.GetDirectoryName(Path);
                if (parent != null && !Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                string content = JsonSerializer.Serialize(Value, _options);
                File.WriteAllText(Path, content);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    // TODO: 释放托管状态(托管对象)
                {
                    Flush();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }
    }
}