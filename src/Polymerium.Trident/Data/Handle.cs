using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static DotNext.Generic.BooleanConst;

namespace Polymerium.Trident.Data
{
    public class Handle<T> : IDisposable
        where T : class
    {
        private JsonSerializerOptions _options;
        private bool disposedValue;

        public T Value { get; private set; }
        public string Path { get; private set; }

        internal Handle(T instance, string path, JsonSerializerOptions? options = null)
        {
            _options = options ?? JsonSerializerOptions.Default;
            Value = instance;
            Path = path;
        }

        public static Handle<T>? Create(string path, JsonSerializerOptions? options = null)
        {
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                var inst = JsonSerializer.Deserialize<T>(content, options);
                if (inst != null)
                    return new Handle<T>(inst, path, options);
            }
            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    var parent = System.IO.Path.GetDirectoryName(Path);
                    if (parent != null && !Directory.Exists(parent))
                        Directory.CreateDirectory(parent);
                    var content = JsonSerializer.Serialize(Value, _options);
                    File.WriteAllText(Path, content);
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~Handle()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
