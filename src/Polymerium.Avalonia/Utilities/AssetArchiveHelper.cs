using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Utilities;

public static class AssetArchiveHelper
{
    public static TMetadata ParsePackMetadata<TMetadata>(string archivePath)
        where TMetadata : IAssetPackMetadataModel, new()
    {
        try
        {
            using var archive = ZipFile.OpenRead(archivePath);
            var packEntry = archive.GetEntry("pack.mcmeta");
            if (packEntry != null)
            {
                return ParsePackMcmeta<TMetadata>(packEntry);
            }
        }
        catch
        {
        }

        return new();
    }

    public static Uri? ExtractIcon(string archivePath, string? entryName)
    {
        if (string.IsNullOrEmpty(entryName))
        {
            return null;
        }

        try
        {
            return ImageSourceHelper.Archive(archivePath, entryName);
        }
        catch
        {
        }

        return null;
    }

    private static TMetadata ParsePackMcmeta<TMetadata>(ZipArchiveEntry entry)
        where TMetadata : IAssetPackMetadataModel, new()
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var metadata = new TMetadata();
        if (root.TryGetProperty("pack", out var packElement))
        {
            if (packElement.TryGetProperty("pack_format", out var formatElement)
             && formatElement.ValueKind == JsonValueKind.Number)
            {
                metadata.PackFormat = formatElement.GetInt32();
            }

            if (packElement.TryGetProperty("description", out var descElement))
            {
                metadata.Description = descElement.ValueKind == JsonValueKind.String
                                           ? descElement.GetString()
                                           : descElement.ToString();
            }
        }

        return metadata;
    }
}
