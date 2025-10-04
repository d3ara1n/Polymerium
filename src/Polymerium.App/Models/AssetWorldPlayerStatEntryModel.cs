using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

/// <summary>
///     玩家统计数据条目模型（用于列表展示）
/// </summary>
public class AssetWorldPlayerStatEntryModel : ModelBase
{
    public AssetWorldPlayerStatEntryModel(string icon, string name, string value)
    {
        Icon = icon;
        Name = name;
        Value = value;
    }

    #region Direct

    public string Icon { get; }
    public string Name { get; }
    public string Value { get; }

    #endregion
}
