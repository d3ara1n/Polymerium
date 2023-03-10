using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.GameAssets;
using Polymerium.Core.Models.Fabric;
using Tomlyn;
using Tomlyn.Model;
using File = System.IO.File;

namespace Polymerium.Core;

// 对游戏本体的操作需要通过 game manager 实施
// 需要 GameInstance 但不关心里面 meta 怎么写，只对本地文件负责
// 维护 GameInstance 的是 InstanceManager(是 asset manager)
// 不包括对游戏运行状态维护，运行状态会在游戏托管后被 GameManagedInstance 代理
public class GameManager
{
    private readonly IFileBaseService _fileBase;

    public GameManager(IFileBaseService fileBase)
    {
        _fileBase = fileBase;
    }

    private IEnumerable<AssetRaw> ScanAssets(GameInstance instance, ResourceType type)
    {
        var dirUri = GetAssetDirectory(instance, type);
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
            try
            {
                using var archive = new ZipArchive(File.OpenRead(fileName), ZipArchiveMode.Read);
                return await (raw.Type switch
                {
                    ResourceType.Mod => ExtractModInfoAsync(archive, raw.FileName, token),
                    ResourceType.ResourcePack => ExtractResourcePackInfoAsync(archive, raw.FileName, token),
                    ResourceType.ShaderPack => Task.FromResult<AssetProduct?>(new AssetProduct
                    {
                        Name = Path.GetFileNameWithoutExtension(fileName),
                        Version = null,
                        Description = null,
                        Type = ResourceType.ShaderPack,
                        FileName = raw.FileName
                    }),
                    _ => throw new NotImplementedException()
                });
            }
            catch
            {
                return null;
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
            var toml = await reader.ReadToEndAsync();
            var info = Toml.ToModel(toml);
            var mod = ((TomlTableArray)info["mods"])?.FirstOrDefault();
            if (mod != null && mod.TryGetValue("displayName", out var name) &&
                mod.TryGetValue("version", out var version) && mod.TryGetValue("authors", out var authors) &&
                mod.TryGetValue("description", out var description))
            {
                var product = new AssetProduct
                {
                    Name = name.ToString()!,
                    Description = description.ToString()!.Trim(),
                    Version = version.ToString()!,
                    Type = ResourceType.Mod,
                    FileName = fileName
                };
                return product;
            }
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

    private async Task<AssetProduct?> ExtractResourcePackInfoAsync(ZipArchive archive, Uri fileName,
        CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return null;
        var meta = archive.GetEntry("pack.mcmeta");
        if (meta != null)
        {
            using var reader = new StreamReader(meta.Open());
            var json = await reader.ReadToEndAsync();
            var info = JsonConvert.DeserializeObject<JObject>(json);
            var description = info?["pack"]?["description"]?.Value<string>();
            if (description != null)
            {
                var product = new AssetProduct
                {
                    Name = Path.GetFileNameWithoutExtension(_fileBase.Locate(fileName)),
                    Description = description,
                    Version = null,
                    FileName = fileName,
                    Type = ResourceType.ResourcePack
                };
                return product;
            }
        }

        return null;
    }

    public async Task<AssetProduct?> InstallAssetAsync(GameInstance instance, ResourceType type, Uri source,
        CancellationToken token = default)
    {
        var real = source.Scheme == "poly-file" ? _fileBase.Locate(source) : source.AbsolutePath;
        var raw = new AssetRaw
        {
            FileName = source,
            Type = type
        };
        var dest = new Uri(GetAssetDirectory(instance, type), Path.GetFileName(real));
        var realDest = _fileBase.Locate(dest);
        var product = await ExtractAssetInfoAsync(raw, token);
        if (product.HasValue && File.Exists(real) && !File.Exists(realDest))
        {
            if (Directory.Exists(Path.GetDirectoryName(realDest)))
                Directory.CreateDirectory(Path.GetDirectoryName(realDest)!);
            File.Copy(real, realDest);
            return product.Value with
            {
                FileName = dest
            };
        }

        return null;
    }

    public Uri GetAssetDirectory(GameInstance instance, ResourceType type)
    {
        var dirUri = new Uri($"poly-file://{instance.Id}/{type switch
        {
            ResourceType.Mod => "mods",
            ResourceType.ShaderPack => "shaderpacks",
            ResourceType.ResourcePack => "resourcepacks",
            _ => throw new NotImplementedException()
        }}/");
        return dirUri;
    }
}