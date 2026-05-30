using System;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncImageLoader.Loaders;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Polymerium.App;

/// <summary>
///     图片加载器，使用独立的 <see cref="MemoryCache" /> 提供带 LRU 驱逐策略的内存缓存。
///     加载失败时记录日志而非静音，缓存驱逐时正确释放 <see cref="Bitmap" /> 的非托管内存。
/// </summary>
public class AppImageLoader(HttpClient httpClient, ILogger<AppImageLoader> logger)
    : BaseWebImageLoader(httpClient, disposeHttpClient: false)
{
    private const int MAX_IMAGE_COUNT = 256;
    private static readonly TimeSpan SLIDING_EXPIRATION = TimeSpan.FromMinutes(30);

    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = MAX_IMAGE_COUNT,
        CompactionPercentage = 0.10,
        ExpirationScanFrequency = TimeSpan.FromMinutes(5)
    });

    public override Task<Bitmap?> ProvideImageAsync(string url) =>
        ProvideImageAsync(url, null);

    public override async Task<Bitmap?> ProvideImageAsync(
        string url,
        IStorageProvider? storageProvider = null
    )
    {
        try
        {
            if (_cache.TryGetValue(url, out Bitmap? cached))
                return cached;

            var bitmap = await LoadAsync(url, storageProvider).ConfigureAwait(false);

            if (bitmap != null)
            {
                _cache.Set(
                    url,
                    bitmap,
                    new MemoryCacheEntryOptions()
                        .SetSize(1)
                        .SetSlidingExpiration(SLIDING_EXPIRATION)
                        .RegisterPostEvictionCallback(OnBitmapEvicted)
                );
            }

            return bitmap;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load image: {Url}", url);
            return null;
        }
    }

    private void OnBitmapEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (value is Bitmap bitmap)
        {
            logger.LogDebug("Bitmap evicted from cache: {Url} ({Reason})", key, reason);
            bitmap.Dispose();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _cache.Dispose();

        base.Dispose(disposing);
    }
}
