using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.Models;

// NOTE: TextBox 文本与 CollectionModel 的双向桥——
//  Convert（Model→文本）回填 Name 供展示；ConvertBack（文本→Model）把键入的名字一次性编码成 Uri 建模。
//  已存在集合经 ListBox 直选 Result，不经此 converter，故其 Uri 保留原样不被重编码。
public sealed class CollectionModelNameConverter : IValueConverter
{
    public static readonly CollectionModelNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CollectionModel m ? m.Name : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        var name = s.Trim();
        return new CollectionModel(name, CollectionHelper.ToUri(name));
    }
}
