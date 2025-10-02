using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Avalonia.Media.Imaging;
using Polymerium.App.Models;
using Tomlyn;
using Tomlyn.Model;

namespace Polymerium.App.Services;

/// <summary>
///     Mod 元数据解析器，支持 Fabric、Forge 和 NeoForge
/// </summary>
public static class ModMetadataHelper
{
    /// <summary>
    ///     从 jar 文件中解析 Mod 元数据
    /// </summary>
    public static AssetModeMetadataModel ParseMetadata(string jarFilePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(jarFilePath);

            // 尝试解析 Fabric Mod (fabric.mod.json)
            var fabricEntry = archive.GetEntry("fabric.mod.json");
            if (fabricEntry != null)
            {
                return ParseFabricMetadata(fabricEntry);
            }

            // 尝试解析 Quilt Mod (quilt.mod.json)
            var quiltEntry = archive.GetEntry("quilt.mod.json");
            if (quiltEntry != null)
            {
                return ParseQuiltMetadata(quiltEntry);
            }

            // 尝试解析 Forge Mod (mods.toml 或 META-INF/mods.toml)
            var forgeEntry = archive.GetEntry("META-INF/mods.toml") ?? archive.GetEntry("mods.toml");
            if (forgeEntry != null)
            {
                return ParseForgeMetadata(forgeEntry);
            }

            // 尝试解析 NeoForge Mod (neoforge.mods.toml 或 META-INF/neoforge.mods.toml)
            var neoforgeEntry = archive.GetEntry("META-INF/neoforge.mods.toml")
                             ?? archive.GetEntry("neoforge.mods.toml");
            if (neoforgeEntry != null)
            {
                return ParseForgeMetadata(neoforgeEntry, ModLoaderKind.NeoForge);
            }

            // 尝试解析旧版 Forge (mcmod.info)
            var legacyForgeEntry = archive.GetEntry("mcmod.info");
            if (legacyForgeEntry != null)
            {
                return ParseLegacyForgeMetadata(legacyForgeEntry);
            }
        }
        catch
        {
            // 解析失败，返回空元数据
        }

        return new();
    }

    /// <summary>
    ///     从 jar 文件中提取 Mod 图标
    /// </summary>
    public static Bitmap? ExtractIcon(string jarFilePath, string? logoFile)
    {
        if (string.IsNullOrEmpty(logoFile))
        {
            return null;
        }

        try
        {
            using var archive = ZipFile.OpenRead(jarFilePath);
            var iconEntry = archive.GetEntry(logoFile);
            if (iconEntry != null)
            {
                using var stream = iconEntry.Open();
                var memory = new MemoryStream();
                stream.CopyTo(memory);
                memory.Position = 0;
                return new(memory);
            }
        }
        catch
        {
            // 图标提取失败
        }

        return null;
    }

    #region Fabric Mod 解析

    private static AssetModeMetadataModel ParseFabricMetadata(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var metadata = new AssetModeMetadataModel
        {
            LoaderType = ModLoaderKind.Fabric,
            ModId = GetJsonString(root, "id"),
            Name = GetJsonString(root, "name"),
            Version = GetJsonString(root, "version"),
            Description = GetJsonString(root, "description")
        };

        // 解析作者
        if (root.TryGetProperty("authors", out var authorsElement))
        {
            metadata.Authors = authorsElement.ValueKind == JsonValueKind.Array
                                   ? authorsElement
                                    .EnumerateArray()
                                    .Select(x => x.GetString() ?? "")
                                    .Where(x => !string.IsNullOrEmpty(x))
                                    .ToArray()
                                   : [authorsElement.GetString() ?? ""];
        }

        // 解析联系信息
        if (root.TryGetProperty("contact", out var contactElement))
        {
            var homepage = GetJsonString(contactElement, "homepage") ?? GetJsonString(contactElement, "sources");
            metadata.Homepage = homepage switch
            {
                not null when homepage.StartsWith("http://") || homepage.StartsWith("https://") => new(homepage),
                _ => null
            };
        }

        // 解析许可证
        metadata.License = GetJsonString(root, "license");

        // 解析图标
        if (root.TryGetProperty("icon", out var iconElement))
        {
            metadata.LogoFile = iconElement.ValueKind == JsonValueKind.String ? iconElement.GetString() :
                                iconElement.ValueKind == JsonValueKind.Object
                             && iconElement.TryGetProperty("512", out var icon512) ? icon512.GetString() :
                                iconElement.TryGetProperty("256", out var icon256) ? icon256.GetString() :
                                iconElement.TryGetProperty("128", out var icon128) ? icon128.GetString() : null;
        }

        return metadata;
    }

    #endregion

    #region Quilt Mod 解析

    private static AssetModeMetadataModel ParseQuiltMetadata(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Quilt 的结构类似 Fabric，但在 quilt_loader 下
        if (root.TryGetProperty("quilt_loader", out var loaderElement))
        {
            var metadata = new AssetModeMetadataModel
            {
                LoaderType = ModLoaderKind.Quilt,
                ModId = GetJsonString(loaderElement, "id"),
                Version = GetJsonString(loaderElement, "version")
            };

            if (loaderElement.TryGetProperty("metadata", out var metadataElement))
            {
                metadata.Name = GetJsonString(metadataElement, "name");
                metadata.Description = GetJsonString(metadataElement, "description");
                metadata.License = GetJsonString(metadataElement, "license");

                if (metadataElement.TryGetProperty("contributors", out var contributorsElement))
                {
                    metadata.Authors = contributorsElement.EnumerateObject().Select(x => x.Name).ToArray();
                }

                if (metadataElement.TryGetProperty("contact", out var contactElement))
                {
                    var homepage = GetJsonString(contactElement, "homepage")
                                ?? GetJsonString(contactElement, "sources");
                    metadata.Homepage = homepage switch
                    {
                        not null when homepage.StartsWith("http://") || homepage.StartsWith("https://") =>
                            new(homepage),
                        _ => null
                    };
                }

                if (metadataElement.TryGetProperty("icon", out var iconElement))
                {
                    metadata.LogoFile = iconElement.ValueKind == JsonValueKind.String
                                            ? iconElement.GetString()
                                            : GetJsonString(iconElement, "512") ?? GetJsonString(iconElement, "256");
                }
            }

            return metadata;
        }

        return new() { LoaderType = ModLoaderKind.Quilt };
    }

    #endregion

    #region Forge/NeoForge Mod 解析 (TOML)

    private static AssetModeMetadataModel ParseForgeMetadata(ZipArchiveEntry entry, ModLoaderKind? guessKind = null)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var tomlContent = reader.ReadToEnd();

        try
        {
            var toml = Toml.ToModel(tomlContent);

            var metadata = new AssetModeMetadataModel { LoaderType = guessKind };

            // 检查是否为 NeoForge
            if (toml.ContainsKey("modLoader")
             && toml["modLoader"].ToString()?.Contains("neoforge", StringComparison.OrdinalIgnoreCase) is true)
            {
                metadata.LoaderType ??= ModLoaderKind.NeoForge;
            }

            metadata.LoaderType ??= ModLoaderKind.Forge;

            // 解析 mods 数组
            if (toml.ContainsKey("mods") && toml["mods"] is TomlTableArray { Count: > 0 } and [{ } modInfo])
            {
                metadata.ModId = modInfo["modId"].ToString();
                metadata.Name = modInfo["displayName"].ToString();
                metadata.Version = modInfo["version"].ToString();
                metadata.Description = modInfo["description"].ToString();
                metadata.Homepage = modInfo["displayURL"].ToString() switch
                {
                    { } it when it.StartsWith("http://") || it.StartsWith("https://") => new(it),
                    _ => null
                };
                metadata.LogoFile = modInfo["logoFile"].ToString();

                var authors = modInfo["authors"].ToString();
                metadata.Authors = authors?.Split(',').Select(x => x.Trim()).ToArray();
            }

            return metadata;
        }
        catch
        {
            return new() { LoaderType = ModLoaderKind.Forge };
        }
    }

    #endregion

    #region 旧版 Forge Mod 解析 (mcmod.info)

    private static AssetModeMetadataModel ParseLegacyForgeMetadata(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // mcmod.info 可能是数组或对象
            var modInfo = root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0 ? root[0] : root;

            var homepage = GetJsonString(modInfo, "url");
            var metadata = new AssetModeMetadataModel
            {
                LoaderType = ModLoaderKind.Forge,
                ModId = GetJsonString(modInfo, "modid"),
                Name = GetJsonString(modInfo, "name"),
                Version = GetJsonString(modInfo, "version"),
                Description = GetJsonString(modInfo, "description"),
                Homepage = homepage switch
                {
                    not null when homepage.StartsWith("http://") || homepage.StartsWith("https://") => new(homepage),
                    _ => null
                },
                LogoFile = GetJsonString(modInfo, "logoFile")
            };

            if (modInfo.TryGetProperty("authorList", out var authorsElement)
             && authorsElement.ValueKind == JsonValueKind.Array)
            {
                metadata.Authors = authorsElement
                                  .EnumerateArray()
                                  .Select(x => x.GetString() ?? "")
                                  .Where(x => !string.IsNullOrEmpty(x))
                                  .ToArray();
            }

            return metadata;
        }
        catch
        {
            return new() { LoaderType = ModLoaderKind.Forge };
        }
    }

    #endregion

    #region 辅助方法

    private static string? GetJsonString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    #endregion
}
