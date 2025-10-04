using IconPacks.Avalonia.Lucide;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

/// <summary>
///     玩家统计数据条目模型（用于列表展示）
/// </summary>
public class AssetWorldPlayerStatEntryModel(PackIconLucideKind icon, string name, string value) : ModelBase
{
    #region Direct

    public PackIconLucideKind Icon { get; } = icon;
    public string Name { get; } = name;
    public string Value { get; } = value;

    #endregion
}
