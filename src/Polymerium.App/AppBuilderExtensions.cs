using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Polymerium.App;

public static class AppBuilderExtensions
{
    public static AppBuilder WithFontSetup(this AppBuilder appBuilder)
    {
        appBuilder
           .ConfigureFonts(fontManager =>
            {
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:Manrope"),
                                                                         new
                                                                             Uri("avares://Polymerium.App/Assets/Fonts/Manrope")));
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:AidianFengYaHei"),
                                                                         new
                                                                             Uri("avares://Polymerium.App/Assets/Fonts/AidianFengYaHei")));
            })
           .With(new FontManagerOptions
            {
                DefaultFamilyName = "fonts:Manrope#Manrope",
                FontFallbacks = [new FontFallback { FontFamily = new FontFamily("fonts:AidianFengYaHei#爱点风雅黑") }]
            });
        return appBuilder;
    }
}