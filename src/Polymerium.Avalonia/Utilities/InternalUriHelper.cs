using System;

namespace Polymerium.Avalonia.Utilities;

/// <summary>
///     Polymerium 内部资源 URI 的判定与构造工具，统一 <c>&lt;kind&gt;://&lt;identifier&gt;</c> 命名规范
///     （<see cref="System.Uri" /> 形态，如 <c>skin://?type=...</c>、<c>recipe://qol-pack</c>）。
///     <para>
///         与 Trident 的 Purl（<c>label:ns/pid@vid</c>，不含 <c>://</c>）天然不重叠：<c>://</c> 是内外分水岭，
///         详见 <c>plans/URL-SCHEME-UNIFICATION.md</c> §3.5。任何"这个 Source 是 recipe 还是整合包"的判断
///         用 <see cref="IsKind" />，不要用 <c>PackageHelper.TryParse</c>。
///     </para>
/// </summary>
public static class InternalUriHelper
{
    public static bool IsInternal(string? s) =>
        s is not null && s.Contains("://", StringComparison.Ordinal);

    public static bool IsKind(string? s, string kind)
    {
        if (s is null)
            return false;

        var prefix = kind + "://";
        return s.StartsWith(prefix, StringComparison.Ordinal);
    }

    public static string Skin(string query) => "skin://" + query;

    public static string Recipe(string id) => "recipe://" + id;
}
