using Polymerium.Trident.Helpers;
using Polymerium.Trident.Models;

namespace Polymerium.Trident.Services;

public class ThumbnailSaver(TridentContext context, IHttpClientFactory factory)
{
    public async Task SaveAsync(string key, Uri url, CancellationToken token = default)
    {
        var target = Path.Combine(context.ThumbnailDir, $"{key}.png");
        await UriFileHelper.SaveAsync(url, target, factory, token);
    }

    public string? Get(string key)
    {
        var path = Path.Combine(context.ThumbnailDir, $"{key}.png");
        if (File.Exists(path))
            return path;
        return null;
    }
}