using System.Text.Json;

namespace Polymerium.Trident.Data
{
    public class Store<T> : IDisposable
        where T : class, new()
    {
        private readonly Handle<T> _handle;
        private bool disposedValue;

        public Store(string path, JsonSerializerOptions? options = null)
        {
            _handle = Handle<T>.Create(path, options) ?? new Handle<T>(new T(), path, options);
        }

        public T Value => _handle.Value;

        public string Path => _handle.Path;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    // TODO: 释放托管状态(托管对象)
                    // Flush
                {
                    _handle.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }
    }
}