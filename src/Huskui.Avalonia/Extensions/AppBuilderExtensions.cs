using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Huskui.Avalonia.Extensions;

public static class AppBuilderExtensions
{
    public static AppBuilder WithOutfitFont(this AppBuilder appBuilder)
    {
        return appBuilder.ConfigureFonts(fontManager =>
        {
            fontManager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:Outfit"),
                new Uri("avares://Huskui.Avalonia/Assets/Fonts/Outfit")));
        }).With(new FontManagerOptions()
        {
            DefaultFamilyName = "fonts:Outfit#Outfit"
        });
    }
}