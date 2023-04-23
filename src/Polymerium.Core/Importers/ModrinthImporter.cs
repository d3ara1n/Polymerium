using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using DotNext;
using Microsoft.Extensions.Caching.Memory;
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
using Polymerium.Core.Models.Modrinth;
using Polymerium.Core.Models.Modrinth.Labrinth;
using Polymerium.Core.Models.Mojang;
using AFile = Polymerium.Abstractions.Resources.File;

namespace Polymerium.Core.Importers;

public class ModrinthImporter : ImporterBase
{
    private readonly IMemoryCache _cache;
    private readonly ResolveEngine _resolver;

    public ModrinthImporter(IMemoryCache cache, ResolveEngine resolver)
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
        var index = JsonConvert.DeserializeObject<ModrinthModpackIndex>(indexContent);
        if (index.Game == "minecraft")
        {
            var metadata = new GameMetadata();
            foreach (var dependency in index.Dependencies)
                metadata.Components.Add(
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
            string? author = null;
            Uri? thumbnail = null;
            if (source != null && !forceOffline)
            {
                var result = await _resolver.ResolveAsync(source, new ResolverContext());
                if (result.IsSuccessful && result.Value.Resource is Modpack modpack)
                {
                    var projectId = modpack.Id;
                    var versionId = modpack.VersionId;
                    var project = await ModrinthHelper.GetProjectAsync(projectId, _cache, Token);
                    var version = await ModrinthHelper.GetVersionAsync(versionId, _cache, Token);
                    if (project.HasValue && version.HasValue)
                    {
                        thumbnail = project.Value.IconUrl;
                        var team = await ModrinthHelper.GetTeamMembersAsync(
                            project.Value.Team,
                            _cache,
                            Token
                        );
                        if (team.Any())
                            author = string.Join(
                                ",",
                                team.Select(
                                    x =>
                                        !string.IsNullOrEmpty(x.User.Name)
                                            ? x.User.Name
                                            : x.User.Username
                                )
                            );
                        var tasks = new List<Task<(LabrinthProject, LabrinthVersion)?>>();
                        // modpack 的 dependency 已经扁平化了，不需要用 ModrinthHelper.ScanDependenciesAsync 去扫
                        foreach (
                            var dependency in version.Value.Dependencies.Where(
                                x => x.VersionId != null || x.ProjectId != null
                            )
                        )
                            tasks.Add(
                                GetDependencyInfoAsync(
                                    dependency.ProjectId,
                                    dependency.VersionId,
                                    _cache,
                                    Token
                                )
                            );
                        await Task.WhenAll(tasks);
                        if (tasks.All(x => x.IsCompletedSuccessfully && x.Result.HasValue))
                        {
                            var externals = tasks.Select(x => x.Result!.Value);
                            var externalFiles = externals.SelectMany(x => x.Item2.Files);
                            // 将 externals 中存在的在 embededs 中移除，根据 url。其实就是用 url 去反查 version 的过程}
                            var embeddeds = index.Files
                                .Where(
                                    x => !x.Downloads.Any(y => externalFiles.Any(z => z.Url == y))
                                )
                                .ToList();
                            // 校验 externals 中没有 versionId 的那部分(requireds)是否存在于 embeddeds。其实不用校验，这是必然的，出问题了责任在打包方。
                            foreach ((var externalProject, var externalVersion) in externals)
                            {
                                var type = ModrinthHelper.GetResourceTypeFromString(
                                    externalProject.ProjectType
                                );
                                var attachment = ModrinthHelper.MakeResourceUrl(
                                    type,
                                    externalProject.Id ?? externalProject.Slug,
                                    externalVersion.Id,
                                    null
                                );
                                metadata.Attachments.Add(
                                    new Attachment() { Source = attachment, From = source }
                                );
                            }
                            AddFilesTo(metadata.Attachments, embeddeds);
                        }
                        else
                            return Failed(GameImportError.ResourceNotFound);
                    }
                    else
                        return Failed(GameImportError.ResourceNotFound);
                }
                else
                    return Failed(GameImportError.ResourceNotFound);
            }
            else
            {
                // 从 index.files 取
                AddFilesTo(metadata.Attachments, index.Files);
            }

            var files = new List<PackedSolidFile>();

            foreach (var file in rawFileList.Where(x => x.StartsWith("overrides")))
                files.Add(
                    new PackedSolidFile
                    {
                        FileName = file,
                        Path = Path.GetRelativePath("overrides", file)
                    }
                );

            foreach (var clientFile in rawFileList.Where(x => x.StartsWith("client-overrides")))
                files.Add(
                    new PackedSolidFile
                    {
                        FileName = clientFile,
                        Path = Path.GetRelativePath("client-overrides", clientFile)
                    }
                );

            return Finished(
                index.Name,
                index.VersionId,
                author ?? string.Empty,
                thumbnail,
                source,
                metadata,
                files
            );
        }

        return Failed(GameImportError.Unsupported);
    }

    private void AddFilesTo(IList<Attachment> container, IEnumerable<ModrinthModpackFile> files)
    {
        foreach (
            var file in files.Where(
                x =>
                    !x.Envs.HasValue
                    || (
                        x.Envs.Value.Client == ModrinthModpackEnv.Optional
                        || x.Envs.Value.Client == ModrinthModpackEnv.Required
                    )
            )
        )
            container.Add(
                new Attachment
                {
                    Source = new Uri(
                        $"poly-res://remote@file/{file.Path}?sha1={file.Hashes.Sha1}&source={HttpUtility.UrlEncode(file.Downloads.First().ToString())}"
                    )
                }
            );
    }

    private async Task<(LabrinthProject, LabrinthVersion)?> GetDependencyInfoAsync(
        string? projectId,
        string? versionId,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        if (versionId != null)
        {
            var version = await ModrinthHelper.GetVersionAsync(versionId, cache, token);
            if (version.HasValue)
            {
                var project = await ModrinthHelper.GetProjectAsync(
                    version.Value.ProjectId,
                    cache,
                    token
                );
                if (project.HasValue)
                    return (project.Value, version.Value);
            }
        }
        else if (projectId != null)
        {
            var project = await ModrinthHelper.GetProjectAsync(projectId, cache, token);
            if (project.HasValue)
            {
                var version = await ModrinthHelper.GetVersionAsync(
                    project.Value.Versions.Last(),
                    cache,
                    token
                );
                if (version.HasValue)
                    return (project.Value, version.Value);
            }
        }
        return null;
    }
}
