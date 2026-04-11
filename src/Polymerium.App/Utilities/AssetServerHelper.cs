using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using fNbt;
using Polymerium.App.Models;

namespace Polymerium.App.Services;

public static class AssetServerHelper
{
    public static IReadOnlyList<AssetServerMetadataModel> ParseMetadata(string serversDatPath)
    {
        var servers = new List<AssetServerMetadataModel>();

        try
        {
            if (!File.Exists(serversDatPath))
            {
                return servers;
            }

            var nbtFile = new NbtFile();
            using (var stream = File.OpenRead(serversDatPath))
            {
                nbtFile.LoadFromStream(stream, NbtCompression.AutoDetect);
            }

            var rootTag = nbtFile.RootTag;
            var serversTag = rootTag.Get<NbtList>("servers");
            if (serversTag == null)
            {
                return servers;
            }

            foreach (var tag in serversTag)
            {
                if (tag is not NbtCompound serverTag)
                {
                    continue;
                }

                servers.Add(
                    new()
                    {
                        Name = serverTag.Get<NbtString>("name")?.Value,
                        Ip = serverTag.Get<NbtString>("ip")?.Value,
                        IconBase64 = serverTag.Get<NbtString>("icon")?.Value,
                        AcceptTextures = serverTag.Get<NbtByte>("acceptTextures")?.Value switch
                        {
                            0 => false,
                            1 => true,
                            _ => null,
                        },
                    }
                );
            }
        }
        catch
        {
            // 解析失败时返回已成功读取的数据
        }

        return servers;
    }

    public static Bitmap? ExtractIcon(string? iconBase64)
    {
        if (string.IsNullOrWhiteSpace(iconBase64))
        {
            return null;
        }

        try
        {
            var payload = iconBase64;
            var commaIndex = payload.IndexOf(',');
            if (commaIndex >= 0)
            {
                payload = payload[(commaIndex + 1)..];
            }

            var bytes = Convert.FromBase64String(payload);
            using var memory = new MemoryStream(bytes);
            return new(memory);
        }
        catch
        {
            return null;
        }
    }
}
