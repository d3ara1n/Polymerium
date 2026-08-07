using Avalonia.Media.Imaging;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

/// <summary>
///     存档中的数据包模型（只读，用于展示）
/// </summary>
public class AssetWorldDataPackModel : ModelBase
{
    public AssetWorldDataPackModel(
        string name,
        string fileName,
        Bitmap icon,
        string? description,
        int? packFormat,
        bool isEnabled)
    {
        Name = name;
        FileName = fileName;
        Icon = icon;
        Description = description;
        PackFormat = packFormat;
        IsEnabled = isEnabled;
    }

    #region Direct

    public string Name { get; }
    public string FileName { get; }
    public Bitmap Icon { get; }
    public string? Description { get; }
    public int? PackFormat { get; }
    public bool IsEnabled { get; }

    public string DisplayName => Name;
    public string PackFormatText => PackFormat?.ToString() ?? LanguageManager.Instance.Enum_Unknown.Current();
    public string DescriptionText => Description ?? LanguageManager.Instance.Enum_Unknown.Current();

    #endregion
}
