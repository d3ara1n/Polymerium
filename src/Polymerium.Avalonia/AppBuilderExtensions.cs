using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Polymerium.Avalonia;

public static class AppBuilderExtensions
{
    public static AppBuilder WithFontSetup(this AppBuilder appBuilder)
    {
        appBuilder
           .ConfigureFonts(fontManager =>
            {
                fontManager.AddFontCollection(new EmbeddedFontCollection(new("fonts:AlimamaFangYuanTi"),
                                                                         new("avares://Polymerium/Assets/Fonts/AlimamaFangYuanTi")));
            })
           .With(new FontManagerOptions { DefaultFamilyName = "fonts:AlimamaFangYuanTi#AlimamaFangYuanTi" });
        return appBuilder;
    }
}
