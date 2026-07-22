using System;

namespace Polymerium.Avalonia.Utilities;

/// <summary>
///     Polymerium 内部资源 URI 的构造与按 scheme 判定工具，统一 <c>&lt;kind&gt;://&lt;identifier&gt;</c>
///     命名规范（如 <c>skin://?type=...</c>、<c>recipe://qol-pack</c>）。
///     <para>
///         注意：Trident 的包标识 Pref 同样是 <c>://</c> 形态（<c>pref://repository/...</c>），
///         与内部资源同形不同 scheme。因此"这个 Source 是 recipe 还是包"必须用 <see cref="IsKind" />
///         按具体 scheme 区分（recipe / pref），不能用"是否含 <c>://</c>"一刀切。
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

    public static string Skin(string query) => "skin://" + query;

    public static string Recipe(string id) => "recipe://" + id;
}
