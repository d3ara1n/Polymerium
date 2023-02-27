using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Importers;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;
using Polymerium.Core.Helpers;
using Polymerium.Core.Models.CurseForge;

namespace Polymerium.Core.Importers;

public class CurseForgeImporter : ImporterBase
{
    public override async Task<Result<ImportResult, GameImportError>> ProcessAsync(ZipArchive archive)
    {
        var indexFile = archive.GetEntry("manifest.json");
        if (indexFile != null)
        {
            using var reader = new StreamReader(indexFile.Open());
            var index = JsonConvert.DeserializeObject<CurseForgeModpackIndex>(await reader.ReadToEndAsync());
            if (index.ManifestType == "minecraftModpack")
            {
                var instance = new GameInstance(new GameMetadata(), index.Version,
                    new FileBasedLaunchConfiguration(), index.Name, index.Name)
                {
                    Author = index.Author
                };
                instance.Metadata.Components.Add(new Component
                {
                    Identity = "net.minecraft",
                    Version = index.Minecraft.Version
                });
                foreach (var modLoader in index.Minecraft.ModLoaders)
                {
                    var split = modLoader.Id.Split("-");
                    if (split.Length > 1)
                    {
                        var name = split[0] switch
                        {
                            "forge" => "net.minecraftforge",
                            "fabric" => "net.fabricmc.fabric-loader",
                            _ => null
                        };
                        if (name == null) return Failed(GameImportError.Unsupported);
                        var version = split[1];
                        instance.Metadata.Components.Add(new Component
                        {
                            Identity = name!,
                            Version = version
                        });
                    }
                    else
                    {
                        return Failed(GameImportError.BrokenIndex);
                    }
                }

                var tasks = index.Files.Where(x => x.Required).Select(x =>
                    CurseForgeHelper.GetModFileInfoAsync(x.ProjectId, x.FileId, Token)
                );

                Task.WaitAll(tasks.ToArray());

                foreach (var task in tasks)
                {
                    var option = task.Result;
                    if (option.TryUnwrap(out var info))
                    {
                        var url = info.DownloadUrl ??
                                  $"https://edge.forgecdn.net/files/{info.Id / 1000}/{info.Id % 1000}/{info.FileName}";
                        string? sha1 = null;
                        var hashes = info.Hashes.Where(x => x.Algo == 2);
                        if (hashes.Any()) sha1 = hashes.First().Value;

                        var res = new Uri(
                            $"poly-res://remote@file/mods/{info.FileName}?{(sha1 != null ? $"sha1={sha1}&" : "")}source={HttpUtility.UrlEncode(url)}"
                        );

                        instance.Metadata.Attachments.Add(res);
                    }
                    else
                    {
                        return Failed(GameImportError.ResourceNotFound);
                    }
                }

                var files = new List<PackedSolidFile>();

                foreach (
                    var file in archive.Entries.Where(
                        x => x.FullName.StartsWith(index.Overrides) && !x.FullName.EndsWith("/")
                    )
                )
                    files.Add(
                        new PackedSolidFile
                        {
                            FileName = file.FullName,
                            Path = Path.GetRelativePath(index.Overrides, file.FullName)
                        }
                    );
                return Finished(archive, instance, files);
            }

            return Failed(GameImportError.Unsupported);
        }

        return Failed(GameImportError.WrongPackType);
    }
}