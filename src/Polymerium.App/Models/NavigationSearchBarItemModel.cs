namespace Polymerium.App.Models;

public class NavigationSearchBarItemModel
{
    public NavigationSearchBarItemModel(
        string caption,
        string glyph,
        string? reference = null,
        string? query = null
    )
    {
        Reference = reference;
        Caption = caption;
        Glyph = glyph;
        Query = query;
    }

    public string? Query { get; set; }
    public string? Reference { get; set; }
    public string Caption { get; set; }
    public string Glyph { get; set; }
}
