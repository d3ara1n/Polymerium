using System;
using System.Reflection.Metadata;

namespace Polymerium.App.Models;

public class NavigationItemModel
{
    public NavigationItemModel(string glyph, string caption, Type sourcePage, object parameter = null, string thumbnailSource = null)
    {
        Glyph = glyph;
        Caption = caption;
        SourcePage = sourcePage;
        PageParameter = parameter;
        ThumbnailSource = thumbnailSource;
    }

    public string Glyph { get; set; }
    public string ThumbnailSource { get; set; }
    public string Caption { get; set; }
    public Type SourcePage { get; set; }
    public object PageParameter { get; set; }
    public bool IsSelected { get; set; }
}
