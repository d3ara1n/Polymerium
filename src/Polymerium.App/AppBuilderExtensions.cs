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
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:HuXiaoBo"),
                                                                         new
                                                                             Uri("avares://Polymerium.App/Assets/Fonts/HuXiaoBo")));
            })
           .With(new FontManagerOptions
            {
                DefaultFamilyName = "fonts:BaiJamjuree#Bai Jamjuree",
                FontFallbacks = [new FontFallback { FontFamily = new FontFamily("fonts:HuXiaoBo#胡晓波真帅体2.0") }]
            });

        return appBuilder;
    }
}