using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Extensions.Logging;
using Polymerium.Avalonia.Rendering;
using SkiaSharp;

namespace Polymerium.Avalonia.Services;

/// <summary>
///     取得原始皮肤后本地渲染为独立位图，由图片加载管线接管所有权。
///     <para>
///         数据源三路：<c>mojang:{uuid}</c>（经第三方皮肤镜像下载原始皮肤 PNG）、
///         裸 http(s) URL（直接下载，供 Authlib 账户使用）、<c>asset:{key}</c>（内置默认皮肤）。
///         任一来源失败一律回落内置 Steve，保证视觉不空缺。
///     </para>
/// </summary>
public sealed class SkinRenderService(HttpClient httpClient, SkinRenderer renderer, ILogger<SkinRenderService> logger)
{
    private const string SteveAssetUri = "avares://Polymerium/Assets/Images/Skins/Steve.png";
    private const string AlexAssetUri = "avares://Polymerium/Assets/Images/Skins/Alex.png";
    private const string HerobrineAssetUri = "avares://Polymerium/Assets/Images/Skins/Herobrine.png";

    /// <summary>
    ///     第三方皮肤镜像根：按 UUID 返回原始皮肤 PNG（64×64 展开图），供本地渲染。
    ///     用常量集中便于上游不可用时一键换源。现在使用 mineatar，其原图端点在国内直连可达。
    /// </summary>
    private const string SkinMirrorBase = "https://api.mineatar.io/skin/";

    public async Task<Bitmap?> RenderAsync(SkinViewType view, string source, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var skin = await LoadSkinAsync(source, cancellationToken).ConfigureAwait(false);
            if (skin is null)
            {
                return null;
            }

            var image = await Task.Run(() => Render(view, skin, cancellationToken), cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
            {
                image.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return image;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to render skin: {Source}", source);
            return null;
        }
    }

    private Bitmap Render(SkinViewType view, SKBitmap skin, CancellationToken cancellationToken)
    {
        SKImage image;
        lock (renderer)
        {
            cancellationToken.ThrowIfCancellationRequested();
            image = renderer.Render(skin, view);
        }

        using (image)
        {
            var bitmap = new WriteableBitmap(new PixelSize(image.Width, image.Height), new Vector(96, 96),
                PixelFormat.Rgba8888, AlphaFormat.Premul);
            try
            {
                using var pixels = bitmap.Lock();
                var info = new SKImageInfo(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                if (!image.ReadPixels(info, pixels.Address, pixels.RowBytes, 0, 0))
                {
                    throw new InvalidOperationException("Unable to copy the rendered skin pixels.");
                }

                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }
    }

    private async Task<SKBitmap?> LoadSkinAsync(string src, CancellationToken cancellationToken)
    {
        var bytes = await TryLoadBytesAsync(src, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        bytes ??= TryLoadAsset(SteveAssetUri);
        return bytes is null ? null : SKBitmap.Decode(bytes);
    }

    private async Task<byte[]?> TryLoadBytesAsync(string src, CancellationToken cancellationToken)
    {
        try
        {
            if (src.StartsWith("mojang:", StringComparison.Ordinal))
            {
                return await httpClient
                            .GetByteArrayAsync(SkinMirrorBase + src["mojang:".Length..], cancellationToken)
                            .ConfigureAwait(false);
            }

            if (src.StartsWith("asset:", StringComparison.Ordinal))
            {
                return TryLoadAsset(ResolveAssetUri(src["asset:".Length..]));
            }

            return await httpClient.GetByteArrayAsync(src, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Skin source unavailable, falling back to Steve: {Src}", src);
            return null;
        }
    }

    /// <summary>
    ///     解析 <c>asset:{key}</c> 的 key 为内置皮肤资源 URI：
    ///     <c>Steve</c>/<c>Alex</c>/<c>Herobrine</c>（不区分大小写）各自映射，其余一律回落 Steve。
    /// </summary>
    private static string ResolveAssetUri(string key) =>
        key.ToLowerInvariant() switch
        {
            "alex" => AlexAssetUri,
            "herobrine" => HerobrineAssetUri,
            _ => SteveAssetUri
        };

    private byte[]? TryLoadAsset(string uri)
    {
        try
        {
            using var stream = AssetLoader.Open(new(uri, UriKind.Absolute));
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Built-in skin asset unavailable: {Uri}", uri);
            return null;
        }
    }
}
