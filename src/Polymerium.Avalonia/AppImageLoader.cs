using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncImageLoader.Loaders;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia;

/// <summary>
///     图片加载器，缓存压缩字节（网络响应原样 / 皮肤渲染 PNG）并按字节精确计费，
///     命中时按需解码 <see cref="Bitmap" />，内存里不再长期持有解码后的大对象。
///     可下采样的缩略图另由 <see cref="DataService.GetBitmapAsync" /> 走解码下采样缓存，二者按能否丢精度分治。
///     加载失败时记录日志而非静音，注意：缓存驱逐不会释放 <see cref="Bitmap" />，
///     因为 Bitmap 可能仍被 UI 引用，提前释放会导致 ObjectDisposedException。
///     GC 的 finalizer 会在所有引用消失后自行回收非托管资源。
/// </summary>
public class AppImageLoader(HttpClient httpClient, SkinRenderService skinRenderer, ILogger<AppImageLoader> logger)
    : BaseWebImageLoader(httpClient, disposeHttpClient: false)
{
    private const long SIZE_LIMIT = 128L * 1024 * 1024;
    private static readonly TimeSpan SLIDING_EXPIRATION = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan NEGATIVE_EXPIRATION = TimeSpan.FromMinutes(3);

    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = SIZE_LIMIT,
        CompactionPercentage = 0.10,
        ExpirationScanFrequency = TimeSpan.FromMinutes(5)
    });

    protected override Task<Bitmap?> LoadFromGlobalCache(string url)
    {
        if (_cache.TryGetValue(url, out var cached) && cached is byte[] bytes)
        {
            try
            {
                using var stream = new MemoryStream(bytes);
                return Task.FromResult<Bitmap?>(new Bitmap(stream));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to decode cached image, discarding: {Url}", url);
                _cache.Remove(url);
            }
        }

        return Task.FromResult<Bitmap?>(null);
    }

    protected override async Task<byte[]?> LoadDataFromExternalAsync(string url)
    {
        // NOTE: 负缓存命中——失败的加载结果短期内直接返回 null，避免重复请求网络。
        if (_cache.TryGetValue(url, out NegativeMarker? _))
        {
            return null;
        }

        try
        {
            var bytes = InternalUriHelper.IsKind(url, SkinHelper.Scheme)
                ? await skinRenderer.RenderPngAsync(url).ConfigureAwait(false)
                : await HttpClient.GetByteArrayAsync(url).ConfigureAwait(false);
            if (bytes is null)
            {
                CacheNegative(url);
            }
            return bytes;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load image: {Url}", url);
            CacheNegative(url);
            return null;
        }
    }

    protected override Task SaveToGlobalCache(string url, byte[] imageBytes)
    {
        _cache.Set(url, imageBytes,
            new MemoryCacheEntryOptions().SetSize(imageBytes.Length).SetSlidingExpiration(SLIDING_EXPIRATION));
        return Task.CompletedTask;
    }

    private void CacheNegative(string url) =>
        _cache.Set(url, new NegativeMarker(), new MemoryCacheEntryOptions().SetAbsoluteExpiration(NEGATIVE_EXPIRATION));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cache.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///     负缓存标记：加载失败（网络异常或皮肤渲染失败）会以此占位符写入缓存，
    ///     使后续命中能区分「缓存了失败」与「缓存了成功」，短期内不再重复请求网络。
    /// </summary>
    private sealed record NegativeMarker;
}
