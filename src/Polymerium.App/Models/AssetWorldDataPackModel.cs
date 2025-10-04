using Avalonia.Media.Imaging;
using Polymerium.App.Facilities;
using Resources = Polymerium.App.Properties.Resources;

namespace Polymerium.App.Models;

/// <summary>
///     存档中的数据包模型（只读，用于展示）
/// </summary>
public class AssetWorldDataPackModel : ModelBase
{
    public AssetWorldDataPackModel(string name, string fileName, Bitmap icon, string? description, int? packFormat)
    {
        Name = name;
        FileName = fileName;
        Icon = icon;
        Description = description;
        PackFormat = packFormat;
    }

    #region Direct

    public string Name { get; }
    public string FileName { get; }
    public Bitmap Icon { get; }
    public string? Description { get; }
    public int? PackFormat { get; }

    public string DisplayName => Name;
    public string PackFormatText => PackFormat?.ToString() ?? Resources.Enum_Unknown;
    public string DescriptionText => Description ?? Resources.Enum_Unknown;

    #endregion
}
