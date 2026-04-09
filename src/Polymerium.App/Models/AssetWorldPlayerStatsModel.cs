using System;
using System.Collections.Generic;
using FluentIcons.Common;
using Humanizer;

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
        return Stats.GetValueOrDefault(key, 0);
    }

    /// <summary>
    ///     获取要展示的统计数据列表（用于 UI 展示）
    /// </summary>
    public IReadOnlyList<AssetWorldPlayerStatEntryModel> GetDisplayStats() =>
        new List<AssetWorldPlayerStatEntryModel>
        {
            new(Symbol.Clock, "Play Time", TimeSpan.FromSeconds(PlayTime / 20d).Humanize(2)),
            new(Symbol.HeartOff, "Deaths", Deaths.ToString()),
            new(Symbol.Sanitize, "Mob Kills", MobKills.ToString()),
            new(Symbol.Target, "Player Kills", PlayerKills.ToString()),
            new(Symbol.Run, "Jumps", ((int)JumpCount).ToMetric()),
            new(Symbol.HeartBroken, "Damage Taken", ((int)DamageTaken).ToMetric()),
            new(Symbol.PersonRunning, "Distance Walked", FormatDistance(DistanceWalked)),
            new(Symbol.Flash, "Distance Sprinted", FormatDistance(DistanceSprinted)),
            new(
                Symbol.Timer,
                "Time Since Death",
                TimeSpan.FromSeconds(TimeSinceDeath / 20d).Humanize(maxUnit: TimeUnit.Hour)
            ),
            new(Symbol.BroomSparkle, "Items Enchanted", ItemsEnchanted.ToString()),
            new(Symbol.AnimalRabbit, "Animals Bred", AnimalsBred.ToString()),
            new(Symbol.FoodFish, "Fish Caught", FishCaught.ToString()),
        };

    private static string FormatDistance(long centimeters)
    {
        var meters = centimeters / 100;
        if (meters >= 1000)
        {
            return $"{meters / 1000.0:F1} km";
        }

        return $"{meters} m";
    }
}
