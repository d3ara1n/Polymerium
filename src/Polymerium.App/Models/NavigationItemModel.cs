using System;
using Polymerium.Abstractions;

namespace Polymerium.App.Models;

public class NavigationItemModel
{
    public NavigationItemModel(string glyph, string caption, Type sourcePage, GameInstance? instance = null,
        string? thumbnailSource = null)
    {
        Glyph = glyph;
        Caption = caption;
        SourcePage = sourcePage;
        GameInstance = instance;
        ThumbnailSource = thumbnailSource;
    }

    public string Glyph { get; set; }
    public string? ThumbnailSource { get; set; }
    public string Caption { get; set; }
    public Type SourcePage { get; set; }
    public GameInstance? GameInstance { get; set; }
}