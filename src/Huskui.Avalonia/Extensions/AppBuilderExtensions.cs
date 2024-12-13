﻿using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Huskui.Avalonia.Extensions;

public static class AppBuilderExtensions
{
    public static AppBuilder WithOutfitFont(this AppBuilder appBuilder)
    {
        return appBuilder.ConfigureFonts(fontManager =>
        {
            fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:MaoKenWangXingYuan"),
                new Uri("avares://Huskui.Avalonia/Assets/Fonts/MaoKenWangXingYuan")));
            fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:Outfit"),
                new Uri("avares://Huskui.Avalonia/Assets/Fonts/Outfit")));
            fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:Quicksand"),
                new Uri("avares://Huskui.Avalonia/Assets/Fonts/Quicksand")));
            fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:Raleway"),
                new Uri("avares://Huskui.Avalonia/Assets/Fonts/Raleway")));
        }).With(new FontManagerOptions
        {
            DefaultFamilyName = "fonts:Outfit#Outfit",
            FontFallbacks =
            [
                new FontFallback
                {
                    FontFamily = "fonts:MaoKenWangXingYuan#猫啃忘形圆"
                }
            ]
        });
    }
}