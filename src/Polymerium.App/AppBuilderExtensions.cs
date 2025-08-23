using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Polymerium.App
{
    public static class AppBuilderExtensions
    {
        public static AppBuilder WithFontSetup(this AppBuilder appBuilder)
        {
            appBuilder
               .ConfigureFonts(fontManager =>
                {
                    fontManager.AddFontCollection(new EmbeddedFontCollection(new("fonts:Manrope"),
                                                                             new("avares://Polymerium.App/Assets/Fonts/Manrope")));
                    fontManager.AddFontCollection(new EmbeddedFontCollection(new("fonts:YSHaoShenTi"),
                                                                             new("avares://Polymerium.App/Assets/Fonts/YSHaoShenTi")));
                })
               .With(new FontManagerOptions
                {
                    DefaultFamilyName = "fonts:Manrope#Manrope",
                    FontFallbacks = [new() { FontFamily = new("fonts:YSHaoShenTi#YOUSHEhaoshenti") }]
                });
            return appBuilder;
        }
    }
}
