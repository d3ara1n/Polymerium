using System;
using System.Text.RegularExpressions;

namespace Polymerium.Avalonia.Utilities;

// 在产出物出口（上传的日志、写盘的 Markdown）对完整文本统一脱敏：按形状替换凭据形状的串，
// 并把用户主目录路径折叠为 ~。基于形状而非参数名匹配，覆盖 Minecraft 各类账户的 access token
// 写法（MSA 的 JWT、Yggdrasil 的 32 位 hex、离线账户的 UUID），也避免在源头分散处理时漏过。
public sealed class Redactor
{
    private const string Redacted = "<redacted>";

    // 带连字符 UUID：离线账户 token、player/mod 各种 id。
    private static readonly Regex Uuid =
        new("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
            RegexOptions.Compiled);

    // Microsoft 在线账户的 access token 是三段式 JWT。
    private static readonly Regex Jwt =
        new("eyJ[A-Za-z0-9_-]+\\.eyJ[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+", RegexOptions.Compiled);

    // Yggdrasil（旧 Mojang）账户的 access token 与无连字符 player id 是 32 位连续 hex。
    private static readonly Regex Hex32 = new("[0-9a-fA-F]{32}", RegexOptions.Compiled);

    private readonly string? _userProfilePath;

    private Redactor(string? userProfilePath) => _userProfilePath = userProfilePath;

    public static Redactor Create() =>
        new(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    public string Apply(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var result = Uuid.Replace(input, Redacted);
        result = Jwt.Replace(result, Redacted);
        result = Hex32.Replace(result, Redacted);

        if (!string.IsNullOrEmpty(_userProfilePath))
        {
            result = result.Replace(_userProfilePath, "~", StringComparison.Ordinal);
        }

        return result;
    }
}
