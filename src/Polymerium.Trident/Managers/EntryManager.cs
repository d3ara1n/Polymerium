using Polymerium.Trident.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Trident.Abstractions;

namespace Polymerium.Trident.Managers;

public class EntryManager : IDisposable
{
    private PolymeriumContext _context;
    private JsonSerializerOptions _options;

    private Store<List<Entry>> entryStore;
    public IList<Entry> Entries => entryStore.Value;

    public EntryManager(PolymeriumContext context, JsonSerializerOptions options)
    {
        _context = context;
        _options = options;

        entryStore = new Store<List<Entry>>(context.ManifestFile, options);
    }

    public Handle<Profile>? GetProfile(string key)
        => Handle<Profile>.Create(Path.Combine(_context.InstanceDir, $"{key}.json"), _options);

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
                entryStore.Dispose();
            }

            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
