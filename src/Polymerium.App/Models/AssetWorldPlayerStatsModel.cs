using System.Collections.Generic;

namespace Polymerium.App.Models;

/// <summary>
///     玩家统计数据模型（从 stats/*.json 解析）
/// </summary>
public class AssetWorldPlayerStatsModel
{
    public Dictionary<string, long> Stats { get; set; } = new();

    // 常用统计数据的便捷访问
    public long PlayTime => GetStat("minecraft:custom", "minecraft:play_time");
    public long Deaths => GetStat("minecraft:custom", "minecraft:deaths");
    public long MobKills => GetStat("minecraft:custom", "minecraft:mob_kills");
    public long PlayerKills => GetStat("minecraft:custom", "minecraft:player_kills");
    public long JumpCount => GetStat("minecraft:custom", "minecraft:jump");
    public long DamageTaken => GetStat("minecraft:custom", "minecraft:damage_taken");
    public long DistanceWalked => GetStat("minecraft:custom", "minecraft:walk_one_cm");
    public long DistanceSprinted => GetStat("minecraft:custom", "minecraft:sprint_one_cm");
    public long TimeSinceDeath => GetStat("minecraft:custom", "minecraft:time_since_death");
    public long ItemsEnchanted => GetStat("minecraft:custom", "minecraft:enchant_item");
    public long AnimalsBred => GetStat("minecraft:custom", "minecraft:animals_bred");
    public long FishCaught => GetStat("minecraft:custom", "minecraft:fish_caught");

    private long GetStat(string category, string stat)
    {
        var key = $"{category}:{stat}";
        return Stats.TryGetValue(key, out var value) ? value : 0;
    }

    /// <summary>
    ///     获取要展示的统计数据列表（用于 UI 展示）
    /// </summary>
    public List<AssetWorldPlayerStatEntryModel> GetDisplayStats() =>
        new()
        {
            new("Clock", "Play Time", FormatPlayTime(PlayTime)),
            new("Skull", "Deaths", Deaths.ToString()),
            new("Sword", "Mob Kills", MobKills.ToString()),
            new("Target", "Player Kills", PlayerKills.ToString()),
            new("TrendingUp", "Jumps", FormatNumber(JumpCount)),
            new("Heart", "Damage Taken", FormatNumber(DamageTaken)),
            new("Footprints", "Distance Walked", FormatDistance(DistanceWalked)),
            new("Zap", "Distance Sprinted", FormatDistance(DistanceSprinted)),
            new("Timer", "Time Since Death", FormatPlayTime(TimeSinceDeath)),
            new("Sparkles", "Items Enchanted", ItemsEnchanted.ToString()),
            new("Rabbit", "Animals Bred", AnimalsBred.ToString()),
            new("Fish", "Fish Caught", FishCaught.ToString())
        };

    private static string FormatPlayTime(long ticks)
    {
        var seconds = ticks / 20;
        var hours = seconds / 3600;
        var minutes = seconds % 3600 / 60;

        if (hours > 0)
        {
            return $"{hours}h {minutes}m";
        }

        return $"{minutes}m";
    }

    private static string FormatDistance(long centimeters)
    {
        var meters = centimeters / 100;
        if (meters >= 1000)
        {
            return $"{meters / 1000.0:F1} km";
        }

        return $"{meters} m";
    }

    private static string FormatNumber(long number)
    {
        if (number >= 1000000)
        {
            return $"{number / 1000000.0:F1}M";
        }

        if (number >= 1000)
        {
            return $"{number / 1000.0:F1}K";
        }

        return number.ToString();
    }
}
