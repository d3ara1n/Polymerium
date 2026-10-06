using System;
using System.Collections.Generic;
using System.IO;
using fNbt;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Utilities;

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

                servers.Add(new()
                {
                    Name = serverTag.Get<NbtString>("name")?.Value,
                    Ip = serverTag.Get<NbtString>("ip")?.Value,
                    IconBase64 = serverTag.Get<NbtString>("icon")?.Value,
                    AcceptTextures = serverTag.Get<NbtByte>("acceptTextures")?.Value switch
                    {
                        0 => false,
                        1 => true,
                        _ => null
                    }
                });
            }
        }
        catch
        {
            // 解析失败时静默返回已读到的部分数据——空 catch 是有意的。
        }

        return servers;
    }

    public static Uri? ExtractIcon(string? iconBase64)
    {
        if (string.IsNullOrWhiteSpace(iconBase64))
        {
            return null;
        }

        try
        {
            return ImageSourceHelper.FromBase64(iconBase64);
        }
        catch
        {
            return null;
        }
    }
}
