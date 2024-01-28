using System.Text.Json;
using DotNext;
using DotNext.Collections.Generic;
using Polymerium.Trident.Models.CurseForge;
using Polymerium.Trident.Repositories;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Extractors;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Extractors;

public class CurseForgeExtractor(JsonSerializerOptions options) : IExtractor
{
    private static readonly IDictionary<string, string> MODLOADER_MAPPINGS = new Dictionary<string, string>
    {
        { "forge", Loader.COMPONENT_FORGE },
        { "neoforge", Loader.COMPONENT_NEOFORGE },
        { "fabric", Loader.COMPONENT_FABRIC },
        { "quilt", Loader.COMPONENT_QUILT }
    };

    public string IdenticalFileName => "manifest.json";

    public Task<Result<ExtractedContainer, ExtractError>> ExtractAsync(string manifestContent,
        ExtractorContext context,
        CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return Task.FromResult(new Result<ExtractedContainer, ExtractError>(ExtractError.Cancelled));
        try
        {
            var manifest = JsonSerializer.Deserialize<CurseForgeModpackManifest>(manifestContent, options);
            if (manifest.Equals(default))
                return Task.FromResult(new Result<ExtractedContainer, ExtractError>(ExtractError.BadFormat));
            var loaders = manifest.Minecraft.ModLoaders.Select(x => (ToLoader(x), x.Primary)).ToArray();
            if (loaders.Any(x => x.Item1 == null))
                return Task.FromResult(new Result<ExtractedContainer, ExtractError>(ExtractError.Unsupported));
            var required = new ContainedLayer
            {
                Summary = manifest.Name,
                OverrideDirectoryName = manifest.Overrides
            };
            required.Loaders.AddAll(loaders.Where(x => x.Primary).Select(x => x.Item1!));
            required.Attachments.AddAll(manifest.Files.Where(x => x.Required).Select(ToAttachment));
            var optional = new ContainedLayer
            {
                Summary = $"{manifest.Name}(Optional)"
            };
            optional.Loaders.AddAll(loaders.Where(x => !x.Primary).Select(x => x.Item1!));
            optional.Attachments.AddAll(manifest.Files.Where(x => !x.Required).Select(ToAttachment));
            var container = new ExtractedContainer(manifest.Name, manifest.Minecraft.Version);
            if (required.Loaders.Any() && required.Attachments.Any()) container.Layers.Add(required);
            if (optional.Loaders.Any() && optional.Attachments.Any()) container.Layers.Add(optional);
            return Task.FromResult(new Result<ExtractedContainer, ExtractError>(container));
        }
        catch (JsonException)
        {
            return Task.FromResult(new Result<ExtractedContainer, ExtractError>(ExtractError.BadFormat));
        }
    }

    private Attachment ToAttachment(CurseForgeModpackManifestFile file)
    {
        return new Attachment(RepositoryLabels.CURSEFORGE,
            file.ProjectId.ToString(),
            file.FileId.ToString());
    }

    private Loader? ToLoader(CurseForgeModpackManifestMinecraftModLoader loader)
    {
        var index = loader.Id.IndexOf('-');
        if (index != -1)
        {
            var id = loader.Id[..index];
            var version = loader.Id[(index + 1)..];
            if (MODLOADER_MAPPINGS.TryGetValue(id, out var loaderId)) return new Loader(loaderId, version);
        }

        return null;
    }
}