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
                fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:BaiJamjuree"),
                                                                         new
                                                                             Uri("avares://Huskui.Avalonia/Assets/Fonts/BaiJamjuree")));
            })
           .With(new FontManagerOptions { DefaultFamilyName = "fonts:BaiJamjuree#Bai Jamjuree" });

        return appBuilder;
    }
}