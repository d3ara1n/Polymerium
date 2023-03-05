using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using DotNext;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Importers;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;
using Polymerium.Core.Components;
using Polymerium.Core.Models.Modrinth;

namespace Polymerium.Core.Importers;

public class ModrinthImporter : ImporterBase
{
    public override async Task<Result<ImportResult, GameImportError>> ProcessAsync(
        ZipArchive archive
    )
    {
        var indexFile = archive.GetEntry("modrinth.index.json");
        if (indexFile != null)
        {
            using var reader = new StreamReader(indexFile.Open());
            var index = JsonConvert.DeserializeObject<ModrinthModpackIndex>(
                await reader.ReadToEndAsync()
            );
            if (index.Game == "minecraft")
            {
                var instance = new GameInstance(
                    new GameMetadata(),
                    index.VersionId,
                    new FileBasedLaunchConfiguration(),
                    index.Name,
                    index.Name
                );
                // 由于是本地导入的，所以没有 ReferenceSource，也不上锁
                // 通过下载中心导入的包会是 poly-res://modpack:modrinth/<id|slug>

                // 不，本地导入的只要是有源包都得上锁且补全信息，除非离线导入（强制无源
                // 然而 Modrinth 的 mrpack 本身就是离线包，完全与主站数据解耦，查都查不到，而 CurseForge 是在线包，真头大

                // NOTE: 所以，本地导入的包全是离线，需要后一步的在线（上锁）过程：当整合包来自 ReferenceSource/poly-res://modpack 时会在导入后添加锁和补全在线信息

                foreach (var dependency in index.Dependencies)
                    instance.Metadata.Components.Add(
                        new Component
                        {
                            Version = dependency.Version,
                            Identity = dependency.Id switch
                            {
                                "minecraft" => ComponentMeta.MINECRAFT,
                                "forge" => ComponentMeta.FORGE,
                                "fabric-loader" => ComponentMeta.FABRIC,
                                "quilt-loader" => ComponentMeta.QUILT,
                                _ => dependency.Id
                            }
                        }
                    );

                foreach (
                    var file in index.Files.Where(
                        x =>
                            !x.Envs.HasValue
                            || (
                                x.Envs.HasValue
                                && (
                                    x.Envs.Value.Client == ModrinthModpackEnv.Optional
                                    || x.Envs.Value.Client == ModrinthModpackEnv.Required
                                )
                            )
                    )
                )
                    instance.Metadata.Attachments.Add(
                        new Uri(
                            $"poly-res://remote@file/{file.Path}?sha1={file.Hashes.Sha1}&source={HttpUtility.UrlEncode(file.Downloads.First().ToString())}"
                        )
                    );

                var files = new List<PackedSolidFile>();

                foreach (
                    var file in archive.Entries.Where(
                        x => x.FullName.StartsWith("overrides") && !x.FullName.EndsWith("/")
                    )
                )
                    files.Add(
                        new PackedSolidFile
                        {
                            FileName = file.FullName,
                            Path = Path.GetRelativePath("overrides", file.FullName)
                        }
                    );

                foreach (
                    var clientFile in archive.Entries.Where(
                        x => x.FullName.StartsWith("client-overrides") && !x.FullName.EndsWith("/")
                    )
                )
                    files.Add(
                        new PackedSolidFile
                        {
                            FileName = clientFile.FullName,
                            Path = Path.GetRelativePath("client-overrides", clientFile.FullName)
                        }
                    );

                return Finished(archive, instance, files);
            }

            return Failed(GameImportError.Unsupported);
        }

        return Failed(GameImportError.WrongPackType);
    }
}