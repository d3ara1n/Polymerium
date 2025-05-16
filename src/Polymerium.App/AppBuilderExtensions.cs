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
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:BaiJamjuree"),
                                                                         new
                                                                             Uri("avares://Polymerium.App/Assets/Fonts/BaiJamjuree")));
            })
           .With(new FontManagerOptions
            {
                DefaultFamilyName = "fonts:BaiJamjuree#Bai Jamjuree",
            });

        return appBuilder;
    }
}