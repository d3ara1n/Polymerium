using DotNext.Collections.Generic;
using Polymerium.Trident.Models.Modrinth;
using Polymerium.Trident.Repositories;
using System.Text.Json;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Extractors;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Extractors;

public class ModrinthExtractor() : IExtractor
{
    public string IdenticalFileName => "modrinth.index.json";
    public static readonly JsonSerializerOptions OPTIONS = new(JsonSerializerDefaults.Web);

    private static readonly IDictionary<string, string> MODLOADER_MAPPINGS = new Dictionary<string, string>
    {
        { "forge", Loader.COMPONENT_FORGE },
        { "neoforge", Loader.COMPONENT_NEOFORGE },
        { "fabric-loader", Loader.COMPONENT_FABRIC },
        { "quilt-loader", Loader.COMPONENT_QUILT }
    };

    public Task<ExtractedContainer> ExtractAsync(string manifestContent, ExtractorContext context,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var manifest = JsonSerializer.Deserialize<ModrinthModpackIndex>(manifestContent, OPTIONS);
        if (manifest.Equals(default))
        {
            throw new BadFormatException(IdenticalFileName, 0, 0);
        }

        var version = ExtractVersion(manifest);
        var loader = ExtractLoader(manifest);

        // TODO: 可能存在 "client-overrides" 目录作为客户端整合包的重载目录
        var required = new ContainedLayer() { Summary = manifest.Name, OverrideDirectoryName = "overrides" };
        if (loader != null)
        {
            required.Loaders.Add(loader);
        }
        required.Attachments.AddAll(manifest.Files.Where(x => !x.Envs.HasValue || x.Envs.Value.Client == ModrinthModpackEnv.Required).Select(ToAttachment));
        var optional = new ContainedLayer() { Summary = $"{manifest.Name}(Optional)" };
        optional.Attachments.AddAll(manifest.Files.Where(x => x.Envs.HasValue && x.Envs.Value.Client == ModrinthModpackEnv.Optional).Select(ToAttachment));
        var container = new ExtractedContainer(manifest.Name, version);
        container.Layers.Add(required);
        if (optional.Loaders.Any() && optional.Attachments.Any())
        {
            container.Layers.Add(optional);
        }
        return Task.FromResult(container);
    }

    private string ExtractVersion(ModrinthModpackIndex index)
    {
        if (index.Dependencies.TryGetValue("minecraft", out var version))
        {
            return version;
        }

        throw new BadFormatException(IdenticalFileName, "dependencies.minecraft");
    }

    private Loader? ExtractLoader(ModrinthModpackIndex index)
    {
        var first = index.Dependencies.Keys.FirstOrDefault(x => x != "minecraft");
        if (first != null)
        {
            if (MODLOADER_MAPPINGS.TryGetValue(first, out var name))
            {
                var version = index.Dependencies[first];
                return new Loader(name, version);
            }

            throw new NotSupportedException($"{first} is not supported as a Loader");
        }

        return null;
    }

    private Attachment ToAttachment(ModrinthModpackFile file)
    {
        var download = file.Downloads.First();
        // https://cdn.modrinth.com/data/88888888/versions/88888888/filename.jar
        if (download.Host == "cdn.modrinth.com")
        {
            var path = download.AbsolutePath;
            if (path.Length > 32)
            {
                var projectId = path[6..14];
                var versionId = path[24..32];
                return new Attachment(RepositoryLabels.MODRINTH, projectId, versionId);
            }
        }

        // or dead end
        throw new NotSupportedException($"{file.Path} can not be recognized as an attachment");
    }
}