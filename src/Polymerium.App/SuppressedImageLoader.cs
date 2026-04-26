using System;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncImageLoader.Loaders;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Polymerium.App;

public class SuppressedImageLoader(HttpClient client) : RamCachedWebImageLoader(client, disposeHttpClient: false)
{
    public override Task<Bitmap?> ProvideImageAsync(string url) => ProvideImageAsync(url, null);

    public override async Task<Bitmap?> ProvideImageAsync(string url, IStorageProvider? storageProvider = null)
    {
        try
        {
            return await base.ProvideImageAsync(url);
        }
        catch
        {
            return null;
        }
    }
}
