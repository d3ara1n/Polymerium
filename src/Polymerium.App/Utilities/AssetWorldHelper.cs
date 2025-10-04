using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Media.Imaging;
using Polymerium.App.Assets;
using Polymerium.App.Models;

namespace Polymerium.App.Services;

/// <summary>
///     存档数据解析器
/// </summary>
public static class AssetWorldHelper
{
    /// <summary>
    ///     从存档文件夹中解析元数据
    /// </summary>
    public static AssetWorldMetadataModel ParseMetadata(string worldPath) =>
        // TODO: 实际实现需要解析 level.dat NBT 文件
        // 目前返回假数据
        new()
        {
            LevelName = Path.GetFileName(worldPath),
            GameType = 0, // Survival
            Difficulty = 2, // Normal
            Hardcore = false,
            AllowCommands = true,
            Time = 123456789,
            DayTime = 6000,
            VersionName = "1.20.1",
            VersionId = 3465,
            Seed = 1234567890,
            GeneratorName = "default",
            Raining = false,
            Thundering = false,
            RainTime = 0,
            ThunderTime = 0
        };

    /// <summary>
    ///     从存档文件夹中提取图标（icon.png）
    /// </summary>
    public static Bitmap? ExtractIcon(string worldPath)
    {
        try
        {
            var iconPath = Path.Combine(worldPath, "icon.png");
            if (File.Exists(iconPath))
            {
                return new(iconPath);
            }
        }
        catch
        {
            // 图标提取失败
        }

        return null;
    }

    /// <summary>
    ///     获取存档的最后游玩时间
    /// </summary>
    public static DateTimeOffset GetLastPlayed(string worldPath)
    {
        try
        {
            var levelDatPath = Path.Combine(worldPath, "level.dat");
            if (File.Exists(levelDatPath))
            {
                return File.GetLastWriteTime(levelDatPath);
            }
        }
        catch
        {
            // 获取失败
        }

        return DateTimeOffset.Now;
    }

    /// <summary>
    ///     解析存档中的数据包列表
    /// </summary>
    public static List<AssetWorldDataPackModel> ParseDataPacks(string worldPath)
    {
        var dataPacks = new List<AssetWorldDataPackModel>();
        var datapacksDir = Path.Combine(worldPath, "datapacks");

        if (!Directory.Exists(datapacksDir))
        {
            return dataPacks;
        }

        // TODO: 从 level.dat 中读取启用的数据包列表
        // 目前假设所有数据包都是启用的
        var enabledDataPacks = new HashSet<string>();

        // 扫描 datapacks 目录下的 zip 文件
        foreach (var file in Directory.GetFiles(datapacksDir, "*.zip"))
        {
            var fileInfo = new FileInfo(file);
            var fileName = fileInfo.Name;

            // 使用 AssetDataPackHelper 解析数据包元数据
            var metadata = AssetDataPackHelper.ParseMetadata(file);
            var icon = AssetDataPackHelper.ExtractIcon(file) ?? AssetUriIndex.DirtImageBitmap;

            // 获取显示名称（优先使用元数据中的描述，否则使用文件名）
            var displayName = !string.IsNullOrEmpty(metadata.Description)
                                  ? metadata.Description
                                  : Path.GetFileNameWithoutExtension(fileName);

            dataPacks.Add(new(displayName, fileName, icon, metadata.Description, metadata.PackFormat));
        }

        return dataPacks;
    }

    /// <summary>
    ///     从文件解析 pack.mcmeta
    /// </summary>
    private static AssetDataPackMetadataModel ParsePackMcmetaFromFile(string packMetaPath)
    {
        try
        {
            var json = File.ReadAllText(packMetaPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var metadata = new AssetDataPackMetadataModel();

            if (root.TryGetProperty("pack", out var pack))
            {
                if (pack.TryGetProperty("pack_format", out var packFormat))
                {
                    metadata.PackFormat = packFormat.GetInt32();
                }

                if (pack.TryGetProperty("description", out var description))
                {
                    metadata.Description = description.GetString();
                }
            }

            return metadata;
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    ///     从文件夹中提取图标
    /// </summary>
    private static Bitmap? ExtractIconFromFolder(string folderPath)
    {
        try
        {
            var iconPath = Path.Combine(folderPath, "pack.png");
            if (File.Exists(iconPath))
            {
                return new(iconPath);
            }
        }
        catch
        {
            // 图标提取失败
        }

        return null;
    }

    /// <summary>
    ///     解析存档中的玩家列表（只包含在 Polymerium 账号管理中的玩家）
    /// </summary>
    public static List<AssetWorldPlayerModel> ParsePlayers(string worldPath, IEnumerable<AccountModel> managedAccounts)
    {
        var players = new List<AssetWorldPlayerModel>();

        try
        {
            var statsDir = Path.Combine(worldPath, "stats");
            var advancementsDir = Path.Combine(worldPath, "advancements");

            if (!Directory.Exists(statsDir))
            {
                return players;
            }

            // 获取所有有统计数据的玩家 UUID
            var playerUuids = Directory
                             .GetFiles(statsDir, "*.json")
                             .Select(Path.GetFileNameWithoutExtension)
                             .Where(uuid => uuid != null)
                             .ToHashSet();

            // 只处理在账号管理中的玩家
            foreach (var account in managedAccounts)
            {
                // 将 UUID 转换为文件名格式（带连字符）
                var formattedUuid = FormatUuid(account.Uuid);

                if (playerUuids.Contains(formattedUuid))
                {
                    var stats = ParsePlayerStats(Path.Combine(statsDir, $"{formattedUuid}.json"));
                    var advancements = ParsePlayerAdvancements(Path.Combine(advancementsDir, $"{formattedUuid}.json"));

                    players.Add(new(account.Uuid, account.UserName, account.FaceUrl, stats, advancements));
                }
            }
        }
        catch
        {
            // 解析失败
        }

        return players;
    }

    /// <summary>
    ///     解析玩家统计数据
    /// </summary>
    private static AssetWorldPlayerStatsModel ParsePlayerStats(string statsFilePath)
    {
        var statsModel = new AssetWorldPlayerStatsModel();

        if (!File.Exists(statsFilePath))
        {
            return statsModel;
        }

        try
        {
            var json = File.ReadAllText(statsFilePath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Minecraft stats JSON 结构：
            // {
            //   "stats": {
            //     "minecraft:custom": {
            //       "minecraft:play_time": 123456,
            //       "minecraft:deaths": 5,
            //       ...
            //     },
            //     "minecraft:mined": {
            //       "minecraft:stone": 100,
            //       ...
            //     },
            //     ...
            //   }
            // }

            if (root.TryGetProperty("stats", out var stats))
            {
                // 遍历所有统计类别
                foreach (var category in stats.EnumerateObject())
                {
                    var categoryName = category.Name;

                    // 遍历该类别下的所有统计项
                    foreach (var stat in category.Value.EnumerateObject())
                    {
                        var statName = stat.Name;
                        var statValue = stat.Value;

                        // 构建完整的统计键：category:stat
                        var key = $"{categoryName}:{statName}";

                        // 获取统计值（应该是数字）
                        if (statValue.ValueKind == JsonValueKind.Number)
                        {
                            statsModel.Stats[key] = statValue.GetInt64();
                        }
                    }
                }
            }
        }
        catch
        {
            // 解析失败，返回空的统计数据
        }

        return statsModel;
    }

    /// <summary>
    ///     解析玩家成就数据
    /// </summary>
    private static AssetWorldPlayerAdvancementsModel ParsePlayerAdvancements(string advancementsFilePath) =>
        // TODO: 实际实现需要解析 JSON 文件
        // 目前返回假数据
        new()
        {
            Advancements = new()
            {
                ["minecraft:story/root"] = true,
                ["minecraft:story/mine_stone"] = true,
                ["minecraft:story/upgrade_tools"] = true,
                ["minecraft:story/smelt_iron"] = false,
                ["minecraft:story/obtain_armor"] = false,
                ["minecraft:nether/root"] = false
            }
        };

    /// <summary>
    ///     格式化 UUID（添加连字符）
    /// </summary>
    private static string FormatUuid(string uuid)
    {
        if (uuid.Length == 32)
        {
            // 无连字符格式转换为带连字符格式
            return
                $"{uuid.Substring(0, 8)}-{uuid.Substring(8, 4)}-{uuid.Substring(12, 4)}-{uuid.Substring(16, 4)}-{uuid.Substring(20)}";
        }

        return uuid;
    }

    /// <summary>
    ///     将统计数据转换为展示用的条目列表
    /// </summary>
    public static List<AssetWorldPlayerStatEntryModel> ConvertStatsToEntries(AssetWorldPlayerStatsModel stats) =>
        // 直接使用 Model 中的方法
        stats.GetDisplayStats();

    /// <summary>
    ///     将成就数据转换为展示用的条目列表
    /// </summary>
    public static List<AssetWorldPlayerAdvancementEntryModel> ConvertAdvancementsToEntries(
        AssetWorldPlayerAdvancementsModel advancements) =>
        advancements
           .Advancements
           .Select(kvp => new AssetWorldPlayerAdvancementEntryModel(FormatAdvancementName(kvp.Key), kvp.Value))
           .ToList();

    private static string FormatAdvancementName(string key)
    {
        // 简单的格式化：移除命名空间，替换下划线为空格
        var parts = key.Split(':');
        var name = parts.Length > 1 ? parts[^1] : key;
        return name.Replace('_', ' ').Replace('/', '>');
    }
}
