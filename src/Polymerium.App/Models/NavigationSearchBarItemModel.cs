namespace Polymerium.App.Models;

public class NavigationSearchBarItemModel
{
    public NavigationSearchBarItemModel(string caption, string glyph, string? reference = null)
    {
        Reference = reference;
        Caption = caption;
        Glyph = glyph;
    }

    public string? Reference { get; set; }
    public string Caption { get; set; }
    public string Glyph { get; set; }
}