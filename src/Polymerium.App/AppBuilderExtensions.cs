using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Polymerium.App;

public static class AppBuilderExtensions
{
    public static AppBuilder WithOutfitFont(this AppBuilder appBuilder)
    {
        appBuilder
           .ConfigureFonts(fontManager =>
            {
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:Quicksand"),
                                                                         new
                                                                             Uri("avares://Polymerium.App/Assets/Fonts/Quicksand")));
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:BaiJamjuree"),
                                                                         new
                                                                             Uri("avares://Polymerium.App/Assets/Fonts/BaiJamjuree")));
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:SanJiSuQian"),
                                                                         new
                                                                             Uri("avares://Polymerium.App/Assets/Fonts/SanJiSuQian")));
            })
           .With(new FontManagerOptions
            {
                DefaultFamilyName = "fonts:BaiJamjuree#Bai Jamjuree",
                FontFallbacks = [new FontFallback { FontFamily = new FontFamily("fonts:SanJiSuQian#三极素纤简体") }]
            });

        return appBuilder;
    }
}