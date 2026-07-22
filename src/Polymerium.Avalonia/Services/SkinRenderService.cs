using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Extensions.Logging;
using Polymerium.Avalonia.Rendering;
using Polymerium.Avalonia.Utilities;
using SkiaSharp;

namespace Polymerium.Avalonia.Services;

/// <summary>
///     解析 <c>skin://?type=&amp;src=</c> 形态的 URI，按数据源路由取得原始皮肤 PNG，
///     交由 <see cref="SkinRenderer" /> 本地离线渲染后产出 Avalonia <see cref="Bitmap" />。
///     <para>
///         数据源三路：<c>mojang:{uuid}</c>（经第三方皮肤镜像下载原始皮肤 PNG）、
///         裸 http(s) URL（直接下载，供 Authlib 账户使用）、<c>asset:{key}</c>（内置默认皮肤）。
///         任一来源失败一律回落内置 Steve，保证视觉不空缺。
///     </para>
///     <para>
///         渲染结果由 <see cref="AppImageLoader" /> 以完整 skin:// URL 为 key 写入 MemoryCache，
///         30 分钟滑动过期内同 URL 不重复请求，天然缓解上游服务的速率限制；
///         加载失败同样写入负缓存（3 分钟绝对过期），避免网络不可达时反复重试刷屏。
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

    /// <summary>
    ///     渲染入口：仅处理 <c>skin://</c> URI，其余返回 null 交由上层走默认网络加载。
    /// </summary>
    public async Task<Bitmap?> RenderAsync(string url)
    {
        if (!InternalUriHelper.IsKind(url, "skin"))
        {
            return null;
        }

        try
        {
            var query = ParseQuery(url);
            if (!query.TryGetValue("type", out var type) || !query.TryGetValue("src", out var src))
            {
                return null;
            }

            using var skin = await LoadSkinAsync(src).ConfigureAwait(false);
            return skin is null ? null : RenderToBitmap(type, skin);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to render skin: {Url}", url);
            return null;
        }
    }

    private Bitmap RenderToBitmap(string type, SKBitmap skin)
    {
        SKImage image;
        lock (renderer)
        {
            // SkiaSharp 的位图/画布操作共享底层句柄，串行化渲染以避免并发句柄竞争。
            // 皮肤图很小，锁开销可忽略。
            // type 字符串直接对应 SkinViewType 枚举名（不区分大小写）；
            // 不识别时回落 Body（等距全身），与历史默认行为一致。
            var view = Enum.TryParse<SkinViewType>(type, true, out var v) ? v : SkinViewType.Body;
            image = renderer.Render(skin, view);
        }

        using (image)
        {
            using var data = image.Encode();
            using var stream = data.AsStream();
            return new(stream);
        }
    }

    private async Task<SKBitmap?> LoadSkinAsync(string src)
    {
        var bytes = await TryLoadBytesAsync(src).ConfigureAwait(false);
        bytes ??= TryLoadAsset(SteveAssetUri);
        return bytes is null ? null : SKBitmap.Decode(bytes);
    }

    private async Task<byte[]?> TryLoadBytesAsync(string src)
    {
        try
        {
            if (src.StartsWith("mojang:", StringComparison.Ordinal))
            {
                return await httpClient
                            .GetByteArrayAsync(SkinMirrorBase + src["mojang:".Length..])
                            .ConfigureAwait(false);
            }

            if (src.StartsWith("asset:", StringComparison.Ordinal))
            {
                return TryLoadAsset(ResolveAssetUri(src["asset:".Length..]));
            }

            // 裸 http(s) URL：直接下载原始皮肤展开图（Authlib 账户的 SkinUrl）。
            return await httpClient.GetByteArrayAsync(src).ConfigureAwait(false);
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

    private static Dictionary<string, string> ParseQuery(string url)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var q = url.IndexOf('?');
        if (q < 0)
        {
            return result;
        }

        foreach (var pair in url.Substring(q + 1).Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            result[Uri.UnescapeDataString(pair[..eq])] = Uri.UnescapeDataString(pair[(eq + 1)..]);
        }

        return result;
    }
}
