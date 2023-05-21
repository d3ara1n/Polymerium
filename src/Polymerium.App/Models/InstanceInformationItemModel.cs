namespace Polymerium.App.Models;

public class InstanceInformationItemModel
{
    public InstanceInformationItemModel(string iconGlyph, string caption, string content)
    {
        IconGlyph = iconGlyph;
        Caption = caption;
        Content = content;
    }

    public InstanceInformationItemModel()
    {
        IconGlyph = string.Empty;
        Caption = string.Empty;
        Content = string.Empty;
    }

    public string IconGlyph { get; set; }
    public string Caption { get; set; }
    public string Content { get; set; }
}