using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Polymerium.Trident.Data
{
    public class Store<T> : IDisposable
        where T : class, new()
    {
        private Handle<T> _handle;
        private bool disposedValue;

        public T Value => _handle.Value;

        public string Path => _handle.Path;

        public Store(string path, JsonSerializerOptions? options = null)
        {
            _handle = Handle<T>.Create(path, options) ?? new Handle<T>(new T(), path, options);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    // Flush
                    _handle.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
