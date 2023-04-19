using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Importers;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Components;
using Polymerium.Core.Engines;
using Polymerium.Core.Helpers;
using Polymerium.Core.Models.CurseForge;
using Polymerium.Core.Models.CurseForge.Eternal;

namespace Polymerium.Core.Importers;

public class CurseForgeImporter : ImporterBase
{
    private readonly IMemoryCache _cache;
    private readonly ResolveEngine _resolver;

    public CurseForgeImporter(IMemoryCache cache, ResolveEngine resolver)
    {
        _cache = cache;
        _resolver = resolver;
    }

    public override async Task<Result<ImportResult, GameImportError>> ProcessAsync(
        ZipArchive archive,
        Uri? source,
        bool forceOffline
    )
    {
        var indexFile = archive.GetEntry("manifest.json");
        if (indexFile != null)
        {
            using var reader = new StreamReader(indexFile.Open());
            var index = JsonConvert.DeserializeObject<CurseForgeModpackIndex>(
                await reader.ReadToEndAsync()
            );
            if (index.ManifestType == "minecraftModpack")
            {
                var instance = new GameInstance(
                    new GameMetadata(),
                    index.Version,
                    new FileBasedLaunchConfiguration(),
                    index.Name,
                    index.Name
                )
                {
                    Author = index.Author
                };
                instance.Metadata.Components.Add(
                    new Component
                    {
                        Identity = ComponentMeta.MINECRAFT,
                        Version = index.Minecraft.Version
                    }
                );
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
                        if (name == null)
                            return Failed(GameImportError.Unsupported);
                        var version = split[1];
                        instance.Metadata.Components.Add(
                            new Component { Identity = name!, Version = version }
                        );
                    }
                    else
                    {
                        return Failed(GameImportError.BrokenIndex);
                    }
                }

                instance.ReferenceSource = source;
                if (source != null && !forceOffline)
                {
                    var result = await _resolver.ResolveAsync(
                        source,
                        new ResolverContext(instance)
                    );
                    if (result.IsSuccessful && result.Value.Resource is Modpack modpack)
                    {
                        instance.ReferenceSource = source;
                        instance.ThumbnailFile = modpack.IconSource?.AbsoluteUri;
                        var tasks = new List<Task<(EternalProject, EternalModFile)?>>();
                        foreach (var file in index.Files)
                            tasks.Add(
                                GetDependencyInfoAsync(file.ProjectId, file.FileId, _cache, Token)
                            );
                        await Task.WhenAll(tasks);
                        if (tasks.All(x => x.IsCompletedSuccessfully && x.Result.HasValue))
                        {
                            foreach (var task in tasks)
                            {
                                (var mod, var file) = task.Result!.Value;
                                var type = CurseForgeHelper.GetResourceTypeFromClassId(mod.ClassId);
                                instance.Metadata.Attachments.Add(
                                    new Attachment()
                                    {
                                        Source = CurseForgeHelper.MakeResourceUrl(
                                            type,
                                            mod.Id.ToString(),
                                            file.Id.ToString()
                                        ),
                                        From = source
                                    }
                                );
                            }
                        }
                        else
                            return Failed(GameImportError.ResourceNotFound);
                    }
                    else
                        return Failed(GameImportError.ResourceNotFound);
                }
                else
                {
                    foreach (var file in index.Files)
                    {
                        instance.Metadata.Attachments.Add(
                            new Attachment
                            {
                                Source = CurseForgeHelper.MakeResourceUrl(
                                    ResourceType.File,
                                    file.ProjectId.ToString(),
                                    file.FileId.ToString()
                                )
                            }
                        );
                        ;
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

    private async Task<(EternalProject, EternalModFile)?> GetDependencyInfoAsync(
        uint projectId,
        uint fileId,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        var mod = await CurseForgeHelper.GetModInfoAsync(projectId, cache, token);
        var file = await CurseForgeHelper.GetModFileInfoAsync(projectId, fileId, _cache, token);
        return (mod.HasValue && file.HasValue) ? (mod.Value, file.Value) : null;
    }
}
