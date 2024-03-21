using System.Text.Json;

namespace Polymerium.Trident.Data
{
    public class Store<T>(string path, JsonSerializerOptions? options = null) : IDisposable
        where T : class, new()
    {
        private readonly Handle<T> _handle = Handle<T>.Create(path, options) ?? new Handle<T>(new T(), path, options);

        public T Value => _handle.Value;

        public string Path => _handle.Path;

        public void Dispose()
        {
            _handle.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}