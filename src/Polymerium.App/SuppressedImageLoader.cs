using System;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncImageLoader.Loaders;
using Avalonia.Media.Imaging;

namespace Polymerium.App;

public class SuppressedImageLoader(HttpClient client)
    : RamCachedWebImageLoader(client, disposeHttpClient: false)
{
    public override async Task<Bitmap?> ProvideImageAsync(string url)
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
