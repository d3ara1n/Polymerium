using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

/// <summary>
///     玩家成就条目模型（用于列表展示）
/// </summary>
public class AssetWorldPlayerAdvancementEntryModel : ModelBase
{
    public AssetWorldPlayerAdvancementEntryModel(string name, bool completed)
    {
        Name = name;
        Completed = completed;
    }

    #region Direct

    public string Name { get; }
    public bool Completed { get; }

    #endregion
}
