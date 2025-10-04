namespace Polymerium.App.Models;

/// <summary>
///     存档元数据模型（从 level.dat 解析）
/// </summary>
public class AssetWorldMetadataModel
{
    // 基本信息
    public string? LevelName { get; set; }
    public int GameType { get; set; } // 0=Survival, 1=Creative, 2=Adventure, 3=Spectator
    public int Difficulty { get; set; } // 0=Peaceful, 1=Easy, 2=Normal, 3=Hard
    public bool Hardcore { get; set; }
    public bool AllowCommands { get; set; }

    // 时间信息
    public long Time { get; set; } // 游戏时间（tick）
    public long DayTime { get; set; } // 当天时间（tick）

    // 版本信息
    public string? VersionName { get; set; }
    public int? VersionId { get; set; }

    // 世界生成信息
    public long? Seed { get; set; }
    public string? GeneratorName { get; set; }

    // 其他信息
    public bool Raining { get; set; }
    public bool Thundering { get; set; }
    public int RainTime { get; set; }
    public int ThunderTime { get; set; }
}
