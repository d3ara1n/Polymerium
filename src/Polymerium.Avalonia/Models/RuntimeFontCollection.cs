using System;
using Avalonia.Media.Fonts;

namespace Polymerium.Avalonia.Models;

// NOTE: 运行时选择的外部字体文件——启动时由 AppBuilderExtensions 注册到 FontManager，
//  FontSelection.FromFile 把 glyph typeface 加进此处，经 Scheme#FamilyName 引用。
internal sealed class RuntimeFontCollection : FontCollectionBase
{
    public const string Scheme = "fonts:Runtime";

    public static readonly RuntimeFontCollection Instance = new();

    public override Uri Key { get; } = new(Scheme, UriKind.Absolute);
}
