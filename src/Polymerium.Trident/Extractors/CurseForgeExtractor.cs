using DotNext;
using DotNext.Collections.Generic;
using Polymerium.Trident.Models.CurseForge;
using Polymerium.Trident.Repositories;
using System.Text.Json;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Extractors;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Extractors
{
    public class CurseForgeExtractor() : IExtractor
    {
        public string IdenticalFileName => "manifest.json";
        public static readonly JsonSerializerOptions OPTIONS = new(JsonSerializerDefaults.Web);

        private static readonly IDictionary<string, string> MODLOADER_MAPPINGS = new Dictionary<string, string>
        {
            { "forge", Loader.COMPONENT_FORGE },
            { "neoforge", Loader.COMPONENT_NEOFORGE },
            { "fabric", Loader.COMPONENT_FABRIC },
            { "quilt", Loader.COMPONENT_QUILT }
        };

        public Task<ExtractedContainer> ExtractAsync(string manifestContent,
            ExtractorContext context,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();


            var manifest = JsonSerializer.Deserialize<CurseForgeModpackManifest>(manifestContent, OPTIONS);
            if (manifest.Equals(default))
            {
                throw new BadFormatException(IdenticalFileName, 0, 0);
            }

            var loaders =
                manifest.Minecraft.ModLoaders.Select(x => (ToLoader(x), x.Primary)).ToArray();
            if (loaders.Any(x => x.Item1 == null))
            {
                throw new NotSupportedException();
            }

            ContainedLayer required = new() { Summary = manifest.Name, OverrideDirectoryName = manifest.Overrides };
            required.Loaders.AddAll(loaders.Where(x => x.Primary).Select(x => x.Item1!));
            required.Attachments.AddAll(manifest.Files.Where(x => x.Required).Select(ToAttachment));
            ContainedLayer optional = new() { Summary = $"{manifest.Name}(Optional)" };
            optional.Loaders.AddAll(loaders.Where(x => !x.Primary).Select(x => x.Item1!));
            optional.Attachments.AddAll(manifest.Files.Where(x => !x.Required).Select(ToAttachment));
            ExtractedContainer container = new(manifest.Name, manifest.Minecraft.Version);
            if (required.Loaders.Any() && required.Attachments.Any())
            {
                container.Layers.Add(required);
            }

            if (optional.Loaders.Any() && optional.Attachments.Any())
            {
                container.Layers.Add(optional);
            }

            return Task.FromResult(container);
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
                if (MODLOADER_MAPPINGS.TryGetValue(id, out var loaderId))
                {
                    return new Loader(loaderId, version);
                }
            }

            return null;
        }
    }
}