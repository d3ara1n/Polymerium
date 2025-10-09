using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Avalonia.Media.Imaging;
using Polymerium.App.Models;

namespace Polymerium.App.Services;

/// <summary>
///     资源包元数据解析器
/// </summary>
public static class AssetResourcePackHelper
{
    /// <summary>
    ///     从 zip 文件中解析资源包元数据
    /// </summary>
    public static AssetResourcePackMetadataModel ParseMetadata(string zipFilePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipFilePath);

            // 尝试解析 pack.mcmeta
            var packEntry = archive.GetEntry("pack.mcmeta");
            if (packEntry != null)
            {
                return ParsePackMcmeta(packEntry);
            }
        }
        catch
        {
            // 解析失败，返回空元数据
        }

        return new();
    }

    /// <summary>
    ///     从 zip 文件中提取资源包图标（pack.png）
    /// </summary>
    public static Bitmap? ExtractIcon(string zipFilePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipFilePath);
            var iconEntry = archive.GetEntry("pack.png");
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

    #region pack.mcmeta 解析

    private static AssetResourcePackMetadataModel ParsePackMcmeta(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var metadata = new AssetResourcePackMetadataModel();

        // 解析 pack 对象
        if (root.TryGetProperty("pack", out var packElement))
        {
            // 解析 pack_format
            if (packElement.TryGetProperty("pack_format", out var formatElement)
             && formatElement.ValueKind == JsonValueKind.Number)
            {
                metadata.PackFormat = formatElement.GetInt32();
            }

            // 解析 description
            if (packElement.TryGetProperty("description", out var descElement))
            {
                // description 可能是字符串或复杂的文本组件
                metadata.Description = descElement.ValueKind == JsonValueKind.String
                                           ? descElement.GetString()
                                           : descElement.ToString();
            }
        }

        return metadata;
    }

    #endregion
}
