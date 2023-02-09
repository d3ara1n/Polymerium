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
using Polymerium.Core.Models.Modrinth;

namespace Polymerium.Core.Importers;

public class ModrinthImporter : ImporterBase
{
    public override async Task<Result<ImportResult, GameImportError>> ProcessAsync(ZipArchive archive)
    {
        var indexFile = archive.GetEntry("modrinth.index.json");
        if (indexFile != null)
        {
            using var reader = new StreamReader(indexFile.Open());
            var index = JsonConvert.DeserializeObject<ModrinthModpackIndex>(await reader.ReadToEndAsync());
            if (index.Game != "minecraft") return Failed(GameImportError.WrongPackType);
            var instance = new GameInstance(new GameMetadata(), index.VersionId, new FileBasedLaunchConfiguration(),
                index.Name, index.Name);
            // 由于是本地导入的，所以没有 ReferenceSource，也不上锁
            // 通过下载中心导入的包会是 poly-res://modpack:modrinth/<id|slug>
            foreach (var dependency in index.Dependencies)
                instance.Metadata.Components.Add(new Component
                {
                    Version = dependency.Version,
                    Identity = dependency.Id switch
                    {
                        "minecraft" => "net.minecraft",
                        "forge" => "net.minecraftforge",
                        "fabric-loader" => "net.fabricmc.fabric-loader",
                        "quilt-loader" => "org.quiltmc.quilt-loader",
                        _ => dependency.Id
                    }
                });

            foreach (var file in index.Files.Where(x => !x.Envs.HasValue || (x.Envs.HasValue &&
                                                                             (x.Envs.Value.Client ==
                                                                              ModrinthModpackEnv.Optional ||
                                                                              x.Envs.Value.Client ==
                                                                              ModrinthModpackEnv.Required))))
            {
                instance.Metadata.Attachments.Add(new Uri(
                    $"poly-res://remote@file/{file.Path}?sha1={file.Hashes.Sha1}&source={HttpUtility.UrlEncode(file.Downloads.First().ToString())}"));
            }

            var files = new List<PackedSolidFile>();

            foreach (var file in archive.Entries.Where(x =>
                         x.FullName.StartsWith("overrides") && !x.FullName.EndsWith("/")))
            {
                files.Add(new PackedSolidFile()
                {
                    FileName = file.FullName,
                    Path = Path.GetRelativePath("overrides", file.FullName)
                });
            }

            foreach (var clientFile in archive.Entries.Where(x =>
                         x.FullName.StartsWith("client-overrides") && !x.FullName.EndsWith("/")))
            {
                files.Add(new PackedSolidFile()
                {
                    FileName = clientFile.FullName,
                    Path = Path.GetRelativePath("client-overrides", clientFile.FullName)
                });
            }

            return Finished(archive, instance, files);
        }

        return Failed(GameImportError.WrongPackType);
    }
}