using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DotNext;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Importers;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;
using Polymerium.Core.Components;
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
                    Identity = ComponentMeta.MINECRAFT,
                    Version = index.Minecraft.Version
                });
                foreach (var modLoader in index.Minecraft.ModLoaders)
                {
                    var split = modLoader.Id.Split("-");
                    if (split.Length > 1)
                    {
                        var name = split[0] switch
                        {
                            "forge" => ComponentMeta.FORGE,
                            "fabric" => ComponentMeta.FABRIC,
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

                foreach (var file in index.Files)
                {
                    var modUrl = new Uri($"poly-res://curseforge@mod/{file.ProjectId}?version={file.FileId}");
                    instance.Metadata.Attachments.Add(modUrl);
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