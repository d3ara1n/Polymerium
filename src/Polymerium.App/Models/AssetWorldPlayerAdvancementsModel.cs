using System.Collections.Generic;
using System.Linq;

namespace Polymerium.App.Models;

/// <summary>
///     玩家成就数据模型（从 advancements/*.json 解析）
/// </summary>
public class AssetWorldPlayerAdvancementsModel
{
    public Dictionary<string, bool> Advancements { get; set; } = new();

    public int TotalCount => Advancements.Count;
    public int CompletedCount => Advancements.Values.Count(x => x);
}
