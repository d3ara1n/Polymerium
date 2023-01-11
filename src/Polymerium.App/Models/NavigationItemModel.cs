using System;

namespace Polymerium.App.Models;

public class NavigationItemModel
{
    public NavigationItemModel(string glyph, string caption, Type sourcePage)
    {
        Glyph = glyph;
        Caption = caption;
        SourcePage = sourcePage;
    }

    public string Glyph { get; set; }
    public string Caption { get; set; }
    public Type SourcePage { get; set; }
    public bool IsSelected { get; set; }
}
