using System;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncImageLoader.Loaders;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia;

/// <summary>
///     图片加载器，使用独立的 <see cref="MemoryCache" /> 提供带 LRU 驱逐策略的内存缓存。
///     加载失败时记录日志而非静音，注意：缓存驱逐不会释放 <see cref="Bitmap" />，
///     因为 Bitmap 可能仍被 UI 引用，提前释放会导致 ObjectDisposedException。
///     GC 的 finalizer 会在所有引用消失后自行回收非托管资源。
/// </summary>
public class AppImageLoader(
    HttpClient httpClient,
    SkinRenderService skinRenderer,
    ILogger<AppImageLoader> logger
)
    : BaseWebImageLoader(httpClient, disposeHttpClient: false)
{
    private const int MAX_IMAGE_COUNT = 256;
    private static readonly TimeSpan SLIDING_EXPIRATION = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan NEGATIVE_EXPIRATION = TimeSpan.FromMinutes(3);

    /// <summary>
    ///     负缓存标记：加载失败（网络异常或回落 Steve）会以此占位符写入缓存，
    ///     使后续命中能区分「缓存了失败」与「缓存了成功」，短期内不再重复请求网络。
    /// </summary>
    private sealed record NegativeMarker;

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
            if (_cache.TryGetValue(url, out var cached))
            {
                // 负缓存命中：加载过的失败结果在短期内直接返回 null，避免重复请求网络。
                if (cached is NegativeMarker)
                    return null;
                return cached as Bitmap;
            }

            Bitmap? bitmap;
            try
            {
                bitmap = url.StartsWith("poly://", StringComparison.Ordinal)
                             ? await skinRenderer.RenderAsync(url).ConfigureAwait(false)
                             : await LoadAsync(url, storageProvider).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load image: {Url}", url);
                CacheNegative(url);
                return null;
            }

            if (bitmap != null)
            {
                _cache.Set(
                    url,
                    bitmap,
                    new MemoryCacheEntryOptions()
                        .SetSize(1)
                        .SetSlidingExpiration(SLIDING_EXPIRATION)
                );
            }
            else
            {
                CacheNegative(url);
            }

            return bitmap;
    }

    private void CacheNegative(string url)
    {
        // 负缓存用绝对过期（非滑动）：保证「网络恢复后最多 N 分钟自动重试」的上限确定，
        // 不会因频繁访问该 URL 而被无限续期。不计入 SizeLimit，避免挤占成功条目配额。
        _cache.Set(
            url,
            new NegativeMarker(),
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(NEGATIVE_EXPIRATION)
        );
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _cache.Dispose();

        base.Dispose(disposing);
    }
}
