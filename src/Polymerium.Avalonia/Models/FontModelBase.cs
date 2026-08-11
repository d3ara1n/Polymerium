using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using SkiaSharp;

namespace Polymerium.Avalonia.Models;

// NOTE: FromConfig 永返回非 null——未设置即 DefaultFontModel，作为结果的一种而非空值特例。
public abstract class FontModelBase
{
    private static HashSet<string>? _systemFamilies;

    private protected FontModelBase(FontFamily preview, bool available)
    {
        Preview = preview;
        IsAvailable = available;
    }

    public abstract string Raw { get; }

    public FontFamily Preview { get; }

    public bool IsAvailable { get; }

    // NOTE: 系统已安装字体的 family name 集合（大小写不敏感，SkiaSharp 枚举，启动后不变）。
    public static ICollection<string> SystemFontFamilies => _systemFamilies ??= EnumerateSystemFonts();

    private static HashSet<string> EnumerateSystemFonts()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var family in SKFontManager.Default.GetFontFamilies())
        {
            set.Add(family);
        }

        return set;
    }

    // NOTE: 配置加载："" / null → DefaultFontModel（不是 null）。
    public static FontModelBase FromConfig(string? raw, FontFamily fallback)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return new DefaultFontModel(fallback);
        }

        var hash = raw.IndexOf('#');
        return hash >= 0 ? FromFile(raw[..hash], fallback) : FromSystem(raw, fallback);
    }

    public static SystemFontModel FromSystem(string familyName, FontFamily fallback)
    {
        var family = new FontFamily(familyName);
        // NOTE: 用 TryGetGlyphTypeface 单查而非枚举全部系统字体，避免启动时同步阻塞。
        var available = FontManager.Current.TryGetGlyphTypeface(new(family), out _);
        return new(familyName, available ? family : fallback, available);
    }

    public static FileFontModel FromFile(string path, FontFamily fallback)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new(path, Path.GetFileName(path), fallback, false);
            }

            using var sk = SKTypeface.FromFile(path);
            var familyName = sk.FamilyName;

            using var stream = File.OpenRead(path);
            RuntimeFontCollection.Instance.TryAddGlyphTypeface(stream, out _);

            return new(path, familyName, new($"{RuntimeFontCollection.Scheme}#{familyName}"), true);
        }
        catch
        {
            return new(path, Path.GetFileName(path), fallback, false);
        }
    }
}
