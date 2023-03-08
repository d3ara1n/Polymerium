using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.GameAssets;
using Polymerium.Core.Models.Fabric;
using File = System.IO.File;

namespace Polymerium.Core;

// 对游戏本体的操作需要通过 game manager 实施
// 需要 GameInstance 但不关心里面 meta 怎么写，只对本地文件负责
// 维护 GameInstance 的是 InstanceManager(是 asset manager)
// 还包括了对游戏运行状态的维护
public class GameManager
{
    private readonly IFileBaseService _fileBase;

    public GameManager(IFileBaseService fileBase)
    {
        _fileBase = fileBase;
    }

    private IEnumerable<AssetRaw> ScanAssets(GameInstance instance, ResourceType type)
    {
        var dirUri = new Uri($"poly-file://{instance.Id}/{type switch
        {
            ResourceType.Mod => "mods",
            ResourceType.ShaderPack => "shaderpacks",
            ResourceType.ResourcePack => "resourcepacks",
            _ => throw new NotImplementedException()
        }}/");
        var dir = new DirectoryInfo(_fileBase.Locate(dirUri));
        var files = dir.Exists
            ? dir.GetFiles(ResourceType.Mod == type ? "*.jar" : "*.zip")
            : Enumerable.Empty<FileInfo>();
        var assets = files.Select(x => new AssetRaw
        {
            FileName = new Uri(dirUri, x.Name),
            Type = type
        });
        return assets;
    }

    public IEnumerable<AssetRaw> ScanAssets(GameInstance instance)
    {
        return Enumerable.Empty<AssetRaw>()
            .Concat(ScanAssets(instance, ResourceType.Mod))
            .Concat(ScanAssets(instance, ResourceType.ResourcePack))
            .Concat(ScanAssets(instance, ResourceType.ShaderPack));
    }

    public async Task<AssetProduct?> ExtractAssetInfoAsync(AssetRaw raw, CancellationToken token = default)
    {
        var fileName = _fileBase.Locate(raw.FileName);
        if (File.Exists(fileName))
        {
            var archive = new ZipArchive(File.OpenRead(fileName), ZipArchiveMode.Read);
            return await (raw.Type switch
            {
                ResourceType.Mod => ExtractModInfoAsync(archive, raw.FileName, token),
                _ => throw new NotImplementedException()
            });
        }

        return null;
    }

    private async Task<AssetProduct?> ExtractModInfoAsync(ZipArchive archive, Uri fileName,
        CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return null;
        var forge = archive.GetEntry("META-INF/mods.toml");
        if (forge != null)
        {
            using var reader = new StreamReader(forge.Open());
            var json = await reader.ReadToEndAsync();
            //Toml.ToModel<>(json);
        }

        var fabric = archive.GetEntry("fabric.mod.json") ?? archive.GetEntry("quilt.mod.json");
        if (fabric != null)
        {
            using var reader = new StreamReader(fabric.Open());
            var json = await reader.ReadToEndAsync();
            var info = JsonConvert.DeserializeObject<FabricModInfo>(json);
            var product = new AssetProduct
            {
                Name = info.Name,
                Description = info.Description,
                Version = info.Version,
                Type = ResourceType.Mod,
                FileName = fileName
            };
            return product;
        }

        return null;
    }

}