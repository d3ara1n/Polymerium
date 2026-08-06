using System;

namespace Polymerium.Avalonia.Utilities;

/// <summary>
///     内部资源 URI 的通用识别工具：<see cref="IsKind" /> 按 scheme 判定某个 string 是否属于给定的
///     内部种类（<c>pref</c> / <c>skin</c> / <c>recipe</c>），统一 <c>&lt;kind&gt;://&lt;identifier&gt;</c> 命名规范。
///     <para>
///         注意：Trident 的包标识 Pref 同样是 <c>://</c> 形态（<c>pref://repository/...</c>），
///         与内部资源同形不同 scheme。因此"这个 Source 是 recipe 还是包"必须按 scheme 显式判定，
///         不能用"是否含 <c>://</c>"一刀切。
///     </para>
/// </summary>
public static class InternalUriHelper
{
    public static bool IsKind(string? s, string kind)
    {
        if (s is null)
        {
            return false;
        }

        var prefix = kind + "://";
        return s.StartsWith(prefix, StringComparison.Ordinal);
    }
}
