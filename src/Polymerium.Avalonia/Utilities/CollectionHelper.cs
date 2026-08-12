using System;
using System.Diagnostics.CodeAnalysis;

namespace Polymerium.Avalonia.Utilities;

// NOTE: collection 是不依赖任何外部资源的就地分组——身份即集合名本身，by-name 段后是 URL 编码的名字，
//  解码即用户可见组名。同名即同组（会合并），改名须重写组内所有 Entry 的 Source。
public static class CollectionHelper
{
    public const string Scheme = "collection";

    private const string Prefix = Scheme + "://";

    private const string ByNameSegment = "by-name/";

    public static string ToUri(string name) => Prefix + ByNameSegment + Uri.EscapeDataString(name);

    public static bool TryGetName(string? s, [MaybeNullWhen(false)] out string name)
    {
        name = null;
        if (s is null || !s.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = s[Prefix.Length..];
        if (!rest.StartsWith(ByNameSegment, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            name = Uri.UnescapeDataString(rest[ByNameSegment.Length..]);
        }
        catch
        {
            return false;
        }

        return true;
    }
}
