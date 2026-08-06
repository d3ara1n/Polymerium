using System;
using System.Diagnostics.CodeAnalysis;

namespace Polymerium.Avalonia.Utilities;

/// <summary>
///     皮肤渲染 URI（<c>skin://?type=&amp;src=</c>）的编制与解析工具：把视图类型与皮肤数据源
///     编码进本地 URI，交 <see cref="Services.SkinRenderService" /> 解析后离线渲染。
///     <para>
///         query 格式（type/src 字段及其转义）在此唯一定义，编制与解析成对，改格式只动这里。
///     </para>
/// </summary>
public static class SkinHelper
{
    public const string Scheme = "skin";

    private const string Prefix = Scheme + "://";

    public static string ToUri(string type, string src) =>
        Prefix + "?type=" + Uri.EscapeDataString(type) + "&src=" + Uri.EscapeDataString(src);

    public static bool TryGetQuery(string? s, [MaybeNullWhen(false)] out string type, [MaybeNullWhen(false)] out string src)
    {
        string? t = null;
        string? sr = null;

        if (s is not null && s.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var q = s.IndexOf('?');
            if (q >= 0)
            {
                foreach (var pair in s[(q + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var eq = pair.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }

                    var key = Uri.UnescapeDataString(pair[..eq]);
                    var val = Uri.UnescapeDataString(pair[(eq + 1)..]);
                    if (key.Equals("type", StringComparison.OrdinalIgnoreCase))
                    {
                        t = val;
                    }
                    else if (key.Equals("src", StringComparison.OrdinalIgnoreCase))
                    {
                        sr = val;
                    }
                }
            }
        }

        if (t is null || sr is null)
        {
            type = null!;
            src = null!;
            return false;
        }

        type = t;
        src = sr;
        return true;
    }
}
