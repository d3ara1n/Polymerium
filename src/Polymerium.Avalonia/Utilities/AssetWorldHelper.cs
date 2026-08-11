using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Media.Imaging;
using fNbt;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Utilities;

/// <summary>
///     存档数据解析器
/// </summary>
public static class AssetWorldHelper
{
    /// <summary>
    ///     从存档文件夹中解析元数据
    /// </summary>
    public static AssetWorldMetadataModel ParseMetadata(string worldPath)
    {
        var metadata = new AssetWorldMetadataModel { LevelName = Path.GetFileName(worldPath) };

        try
        {
            var levelDatPath = Path.Combine(worldPath, "level.dat");
            if (!File.Exists(levelDatPath))
            {
                return metadata;
            }

            var nbtFile = new NbtFile();
            using (var stream = File.OpenRead(levelDatPath))
            {
                nbtFile.LoadFromStream(stream, NbtCompression.AutoDetect);
            }

            var rootTag = nbtFile.RootTag;
            var dataTag = rootTag.Get<NbtCompound>("Data");

            if (dataTag == null)
            {
                return metadata;
            }

            metadata.LevelName = dataTag.Get<NbtString>("LevelName")?.Value ?? Path.GetFileName(worldPath);
            metadata.GameType = dataTag.Get<NbtInt>("GameType")?.Value ?? 0;
            metadata.Difficulty = dataTag.Get<NbtByte>("Difficulty")?.Value ?? 2;
            metadata.Hardcore = dataTag.Get<NbtByte>("hardcore")?.Value == 1;
            metadata.AllowCommands = dataTag.Get<NbtByte>("allowCommands")?.Value == 1;

            metadata.Time = dataTag.Get<NbtLong>("Time")?.Value ?? 0;
            metadata.DayTime = dataTag.Get<NbtLong>("DayTime")?.Value ?? 0;

            var versionTag = dataTag.Get<NbtCompound>("Version");
            if (versionTag != null)
            {
                metadata.VersionName = versionTag.Get<NbtString>("Name")?.Value;
                metadata.VersionId = versionTag.Get<NbtInt>("Id")?.Value;
            }

            var worldGenSettings = dataTag.Get<NbtCompound>("WorldGenSettings");
            if (worldGenSettings != null)
            {
                metadata.Seed = worldGenSettings.Get<NbtLong>("seed")?.Value;

                var dimensions = worldGenSettings.Get<NbtCompound>("dimensions");
                var overworldDim = dimensions?.Get<NbtCompound>("minecraft:overworld");
                var generator = overworldDim?.Get<NbtCompound>("generator");
                metadata.GeneratorName = generator?.Get<NbtString>("type")?.Value ?? "default";
            }
            else
            {
                metadata.Seed = dataTag.Get<NbtLong>("RandomSeed")?.Value;
                metadata.GeneratorName = dataTag.Get<NbtString>("generatorName")?.Value ?? "default";
            }

            metadata.Raining = dataTag.Get<NbtByte>("raining")?.Value == 1;
            metadata.Thundering = dataTag.Get<NbtByte>("thundering")?.Value == 1;
            metadata.RainTime = dataTag.Get<NbtInt>("rainTime")?.Value ?? 0;
            metadata.ThunderTime = dataTag.Get<NbtInt>("thunderTime")?.Value ?? 0;

            var dataPacksTag = dataTag.Get<NbtCompound>("DataPacks");
            if (dataPacksTag != null)
            {
                var enabledTag = dataPacksTag.Get<NbtList>("Enabled");
                if (enabledTag != null)
                {
                    foreach (var tag in enabledTag)
                    {
                        if (tag is NbtString stringTag)
                        {
                            metadata.EnabledDataPacks.Add(stringTag.Value);
                        }
                    }
                }
            }
        }
        catch
        {
        }

        return metadata;
    }

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
        }

        return DateTimeOffset.Now;
    }

    /// <summary>
    ///     解析存档中的数据包列表
    /// </summary>
    public static IReadOnlyList<AssetWorldDataPackModel> ParseDataPacks(
        string worldPath,
        AssetWorldMetadataModel metadata)
    {
        var dataPacks = new List<AssetWorldDataPackModel>();
        var datapacksDir = Path.Combine(worldPath, "datapacks");

        if (!Directory.Exists(datapacksDir))
        {
            return dataPacks;
        }

        var enabledDataPacks = metadata.EnabledDataPacks;

        var zipFiles = Directory.GetFiles(datapacksDir, "*.zip");
        foreach (var file in zipFiles)
        {
            var fileInfo = new FileInfo(file);
            var fileName = fileInfo.Name;
            var dataPackName = $"file/{Path.GetFileNameWithoutExtension(fileName)}";

            // NOTE: enabledDataPacks 为空（level.dat 读不出启用列表）时视为全部启用，而非全部禁用。
            var isEnabled = enabledDataPacks.Count == 0 || enabledDataPacks.Contains(dataPackName);

            var packMetadata = AssetDataPackHelper.ParseMetadata(file);
            var icon = AssetDataPackHelper.ExtractIcon(file) ?? AssetUriIndex.DirtImageBitmap;
            var displayName = !string.IsNullOrEmpty(packMetadata.Description)
                                  ? packMetadata.Description
                                  : Path.GetFileNameWithoutExtension(fileName);

            dataPacks.Add(new(displayName,
                              fileName,
                              icon,
                              packMetadata.Description,
                              packMetadata.PackFormat,
                              isEnabled));
        }

        var directories = Directory.GetDirectories(datapacksDir);
        foreach (var dir in directories)
        {
            var dirInfo = new DirectoryInfo(dir);
            var dirName = dirInfo.Name;
            var dataPackName = $"file/{dirName}";

            var isEnabled = enabledDataPacks.Count == 0 || enabledDataPacks.Contains(dataPackName);

            var packMetaPath = Path.Combine(dir, "pack.mcmeta");
            if (!File.Exists(packMetaPath))
            {
                continue;
            }

            var packMetadata = ParsePackMcmetaFromFile(packMetaPath);
            var icon = ExtractIconFromFolder(dir) ?? AssetUriIndex.DirtImageBitmap;
            var displayName = !string.IsNullOrEmpty(packMetadata.Description) ? packMetadata.Description : dirName;

            dataPacks.Add(new(displayName,
                              dirName,
                              icon,
                              packMetadata.Description,
                              packMetadata.PackFormat,
                              isEnabled));
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
        }

        return null;
    }

    /// <summary>
    ///     解析存档中的玩家列表（只包含在 Polymerium 账号管理中的玩家）
    /// </summary>
    public static IReadOnlyList<AssetWorldPlayerModel> ParsePlayers(
        string worldPath,
        IEnumerable<AccountModel> managedAccounts)
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

            var playerUuids = Directory
                             .GetFiles(statsDir, "*.json")
                             .Select(Path.GetFileNameWithoutExtension)
                             .Where(uuid => uuid != null)
                             .ToHashSet();

            foreach (var account in managedAccounts)
            {
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
                foreach (var category in stats.EnumerateObject())
                {
                    var categoryName = category.Name;

                    foreach (var stat in category.Value.EnumerateObject())
                    {
                        var statName = stat.Name;
                        var statValue = stat.Value;

                        var key = $"{categoryName}:{statName}";

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
        }

        return statsModel;
    }

    /// <summary>
    ///     解析玩家成就数据
    /// </summary>
    private static AssetWorldPlayerAdvancementsModel ParsePlayerAdvancements(string advancementsFilePath)
    {
        var advancementsModel = new AssetWorldPlayerAdvancementsModel();

        if (!File.Exists(advancementsFilePath))
        {
            return advancementsModel;
        }

        try
        {
            var json = File.ReadAllText(advancementsFilePath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Minecraft advancements JSON 结构：
            // {
            //   "minecraft:story/root": {
            //     "done": true,
            //     "criteria": {
            //       "crafting_table": "2023-01-01T12:00:00Z"
            //     }
            //   },
            //   "minecraft:story/mine_stone": {
            //     "done": false
            //   },
            //   ...
            // }

            foreach (var advancement in root.EnumerateObject())
            {
                var advancementName = advancement.Name;
                var advancementData = advancement.Value;

                var isDone = false;
                if (advancementData.TryGetProperty("done", out var doneProperty))
                {
                    isDone = doneProperty.GetBoolean();
                }

                advancementsModel.Advancements[advancementName] = isDone;
            }
        }
        catch
        {
        }

        return advancementsModel;
    }

    /// <summary>
    ///     格式化 UUID（添加连字符）
    /// </summary>
    private static string FormatUuid(string uuid)
    {
        if (uuid.Length == 32)
        {
            return $"{uuid[..8]}-{uuid[8..12]}-{uuid[12..16]}-{uuid[16..20]}-{uuid[20..]}";
        }

        return uuid;
    }
}
