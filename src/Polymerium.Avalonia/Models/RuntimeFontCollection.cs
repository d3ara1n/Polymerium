using System;
using Avalonia.Media.Fonts;

namespace Polymerium.Avalonia.Models;

// 承载用户运行时选择的外部字体文件，启动时由 AppBuilderExtensions 注册到 FontManager。
// FontSelection.FromFile 把文件 glyph typeface 加进此处，再用 Scheme#FamilyName 引用。
internal sealed class RuntimeFontCollection : FontCollectionBase
{
    public const string Scheme = "fonts:Runtime";

    public static readonly RuntimeFontCollection Instance = new();

    public override Uri Key { get; } = new(Scheme, UriKind.Absolute);
}
