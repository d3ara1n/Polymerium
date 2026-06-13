using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Extensions.Logging;
using Polymerium.Avalonia.Rendering;
using SkiaSharp;

namespace Polymerium.Avalonia.Services;

/// <summary>
///     解析 <c>poly://skin?type=&amp;src=</c> 形态的 URI，按数据源路由取得原始皮肤 PNG，
///     交由 <see cref="SkinRenderer" /> 本地离线渲染后产出 Avalonia <see cref="Bitmap" />。
///     <para>
///         数据源三路：<c>mojang:{uuid}</c>（查 Mojang sessionserver profile，取 textures.SKIN.url）、
///         裸 http(s) URL（直接下载，供 Authlib 账户使用）、<c>asset:{key}</c>（内置默认皮肤）。
///         任一来源失败一律回落内置 Steve，保证视觉不空缺。
///     </para>
///     <para>
///         渲染结果由 <see cref="AppImageLoader" /> 以完整 poly:// URL 为 key 写入 MemoryCache，
///         30 分钟滑动过期内同 URL 不重复请求 Mojang，天然缓解 sessionserver 的速率限制。
///     </para>
/// </summary>
public sealed class SkinRenderService(
    HttpClient httpClient,
    SkinRenderer renderer,
    ILogger<SkinRenderService> logger)
{
    private const string Scheme = "poly://";
    private const string SteveAssetUri = "avares://Polymerium/Assets/Images/Skins/Steve.png";
    private const string AlexAssetUri = "avares://Polymerium/Assets/Images/Skins/Alex.png";

    private const string MojangProfileApi =
        "https://sessionserver.minecraft.net/session/minecraft/profile/";

    /// <summary>
    ///     渲染入口：仅处理 <c>poly://</c> URI，其余返回 null 交由上层走默认网络加载。
    /// </summary>
    public async Task<Bitmap?> RenderAsync(string url)
    {
        if (!url.StartsWith(Scheme, StringComparison.Ordinal))
            return null;

        try
        {
            var query = ParseQuery(url);
            if (!query.TryGetValue("type", out var type) || !query.TryGetValue("src", out var src))
                return null;

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
            var view = Enum.TryParse<SkinViewType>(type, ignoreCase: true, out var v)
                ? v
                : SkinViewType.Body;
            image = renderer.Render(skin, view);
        }

        using (image)
        {
            using var data = image.Encode();
            using var stream = data.AsStream();
            return new Bitmap(stream);
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
                return await LoadMojangSkinAsync(src["mojang:".Length..]).ConfigureAwait(false);

            if (src.StartsWith("asset:", StringComparison.Ordinal))
                return TryLoadAsset(ResolveAssetUri(src["asset:".Length..]));

            // 裸 http(s) URL：直接下载原始皮肤展开图（Authlib 账户的 SkinUrl）。
            return await httpClient.GetByteArrayAsync(src).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Skin source unavailable, falling back to Steve: {Src}", src);
            return null;
        }
    }

    private async Task<byte[]?> LoadMojangSkinAsync(string uuid)
    {
        var profileUrl = new Uri(MojangProfileApi + uuid, UriKind.Absolute);
        var profileJson = await httpClient.GetStringAsync(profileUrl).ConfigureAwait(false);
        var texturesBase64 = ExtractTexturesValue(profileJson);
        if (texturesBase64 is null)
            return null;

        var texturesJson = DecodeBase64(texturesBase64);
        var skinUrl = ExtractSkinUrl(texturesJson);
        return skinUrl is null
            ? null
            : await httpClient.GetByteArrayAsync(skinUrl).ConfigureAwait(false);
    }

    private static string? ExtractTexturesValue(string profileJson)
    {
        using var doc = JsonDocument.Parse(profileJson);
        if (!doc.RootElement.TryGetProperty("properties", out var props))
            return null;

        foreach (var p in props.EnumerateArray())
        {
            if (p.TryGetProperty("name", out var name)
                && name.ValueKind == JsonValueKind.String
                && name.GetString() == "textures"
                && p.TryGetProperty("value", out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static string? ExtractSkinUrl(string texturesJson)
    {
        using var doc = JsonDocument.Parse(texturesJson);
        if (doc.RootElement.TryGetProperty("textures", out var textures)
            && textures.TryGetProperty("SKIN", out var skin)
            && skin.TryGetProperty("url", out var url)
            && url.ValueKind == JsonValueKind.String)
        {
            return url.GetString();
        }

        return null;
    }

    private static string DecodeBase64(string base64)
    {
        // Mojang textures value 为标准 base64，兼容偶发缺失 padding 的情况。
        var rem = base64.Length % 4;
        if (rem != 0)
            base64 += new string('=', 4 - rem);
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    /// <summary>
    ///     解析 <c>asset:{key}</c> 的 key 为内置皮肤资源 URI：
    ///     <c>Steve</c>/<c>Alex</c>（不区分大小写）各自映射，其余一律回落 Steve。
    /// </summary>
    private static string ResolveAssetUri(string key) => key.ToLowerInvariant() switch
    {
        "alex" => AlexAssetUri,
        _ => SteveAssetUri,
    };

    private byte[]? TryLoadAsset(string uri)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(uri, UriKind.Absolute));
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
            return result;

        foreach (var pair in url.Substring(q + 1).Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0)
                continue;
            result[Uri.UnescapeDataString(pair[..eq])] =
                Uri.UnescapeDataString(pair[(eq + 1)..]);
        }

        return result;
    }
}
