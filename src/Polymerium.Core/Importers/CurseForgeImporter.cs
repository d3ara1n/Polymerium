using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Polymerium.Abstractions.Importers;
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

    public override async Task<Result<ModpackContent, GameImportError>> ExtractMetadataAsync(
        string indexContent,
        IEnumerable<string> rawFileList,
        Uri? source,
        bool forceOffline
    )
    {
        var index = JsonConvert.DeserializeObject<CurseForgeModpackIndex>(indexContent);
        if (index.ManifestType == "minecraftModpack")
        {
            var metadata = new GameMetadata();
            metadata.Components.Add(
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
                    metadata.Components.Add(new Component { Identity = name!, Version = version });
                }
                else
                {
                    return Failed(GameImportError.BrokenIndex);
                }
            }

            Uri? thumbnail = null;

            if (source != null && !forceOffline)
            {
                var result = await _resolver.ResolveAsync(source, new ResolverContext());
                if (result.IsSuccessful && result.Value.Resource is Modpack modpack)
                {
                    thumbnail = modpack.IconSource;
                    var tasks = new List<Task<(EternalProject, EternalModFile)?>>();
                    foreach (var file in index.Files)
                        tasks.Add(
                            GetDependencyInfoAsync(file.ProjectId, file.FileId, _cache, Token)
                        );
                    await Task.WhenAll(tasks);
                    if (tasks.All(x => x.IsCompletedSuccessfully && x.Result.HasValue))
                        foreach (var task in tasks)
                        {
                            var (mod, file) = task.Result!.Value;
                            var type = CurseForgeHelper.GetResourceTypeFromClassId(mod.ClassId);
                            metadata.Attachments.Add(
                                new Attachment
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
                    else
                        return Failed(GameImportError.ResourceNotFound);
                }
                else
                {
                    return Failed(GameImportError.ResourceNotFound);
                }
            }
            else
            {
                foreach (var file in index.Files)
                {
                    metadata.Attachments.Add(
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

            foreach (var file in rawFileList.Where(x => x.StartsWith(index.Overrides)))
                files.Add(
                    new PackedSolidFile
                    {
                        FileName = file,
                        Path = Path.GetRelativePath(index.Overrides, file)
                    }
                );
            return Finished(
                index.Name,
                index.Version,
                index.Author,
                thumbnail,
                source,
                metadata,
                files
            );
        }

        return Failed(GameImportError.Unsupported);
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
        return mod.HasValue && file.HasValue ? (mod.Value, file.Value) : null;
    }
}