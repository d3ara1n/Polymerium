using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Huskui.Avalonia.Extensions;

public static class AppBuilderExtensions
{
    public static AppBuilder WithOutfitFont(this AppBuilder appBuilder)
    {
        appBuilder
           .ConfigureFonts(fontManager =>
            {
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:Quicksand"),
                                                                         new
                                                                             Uri("avares://Huskui.Avalonia/Assets/Fonts/Quicksand")));
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:MaoKenWangXingYuan"),
                                                                         new
                                                                             Uri("avares://Huskui.Avalonia/Assets/Fonts/MaoKenWangXingYuan")));
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:ChillRoundM"),
                                                                         new
                                                                             Uri("avares://Huskui.Avalonia/Assets/Fonts/ChillRoundM")));
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:BaiJamjuree"),
                                                                         new
                                                                             Uri("avares://Huskui.Avalonia/Assets/Fonts/BaiJamjuree")));
            })
           .With(new FontManagerOptions
            {
                DefaultFamilyName = "fonts:Quicksand#Quicksand",
                FontFallbacks =
                [
                    new FontFallback { FontFamily = "fonts:ChillRoundM#寒蝉半圆体" },
                    // new FontFallback { FontFamily = "fonts:MaoKenWangXingYuan#猫啃忘形圆" }
                ]
            });

        return appBuilder;
    }
}