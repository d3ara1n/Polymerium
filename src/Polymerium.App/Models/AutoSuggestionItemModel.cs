namespace Polymerium.App.Models;

public class AutoSuggestionItemModel
{
    public AutoSuggestionItemModel(string caption, string glyph, string thumbnail)
    {
        Caption = caption;
        Glyph = glyph;
        Thumbnail = thumbnail;
    }

    public string Caption { get; set; }
    public string Glyph { get; set; }
    public string Thumbnail { get; set; }
}
