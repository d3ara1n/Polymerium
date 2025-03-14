﻿using System.Text.Json;
using Polymerium.Trident.Models.CurseForgePack;
using Polymerium.Trident.Services;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Importers;
using Trident.Abstractions.Utilities;

namespace Polymerium.Trident.Importers;

public class CurseForgeImporter : IProfileImporter
{
    private static readonly Dictionary<string, string> LOADER_MAPPINGS = new()
    {
        ["forge"] = LoaderHelper.LOADERID_FORGE,
        ["neoforge"] = LoaderHelper.LOADERID_NEOFORGE,
        ["fabric"] = LoaderHelper.LOADERID_FABRIC,
        ["quilt"] = LoaderHelper.LOADERID_QUILT
    };

    private bool TryExtractLoader(
        IEnumerable<ManifestModel.MinecraftModel.ModLoaderModel> loaders,
        out (string Identity, string Version) loader)
    {
        var primary = loaders.FirstOrDefault(x => x.Primary);
        loader = default((string Identity, string Version));
        if (primary is null || !primary.Id.Contains('-'))
            return false;

        var name = primary.Id[..primary.Id.IndexOf('-')];
        var ver = primary.Id[(primary.Id.IndexOf('-') + 1)..];
        if (LOADER_MAPPINGS.TryGetValue(name, out var mapping))
            name = mapping;

        loader = (name, ver);
        return true;
    }


    #region IProfileImporter Members

    public string IndexFileName { get; } = "manifest.json";

    public async Task<ImportedProfileContainer> ExtractAsync(CompressedProfilePack pack)
    {
        await using var manifestStream = pack.Open(IndexFileName);
        var manifest = await JsonSerializer.DeserializeAsync<ManifestModel>(manifestStream, JsonSerializerOptions.Web);
        if (manifest is null || !TryExtractLoader(manifest.Minecraft.ModLoaders, out var loader))
            throw new FormatException($"{IndexFileName} is not a valid manifest");

        return new ImportedProfileContainer(new Profile(manifest.Name,
                                                        new Profile.Rice(pack.Reference is not null
                                                                             ? PackageHelper.ToPurl(pack.Reference)
                                                                             : null,
                                                                         manifest.Minecraft.Version,
                                                                         LoaderHelper.ToLurl(loader.Identity,
                                                                             loader.Version),
                                                                         manifest
                                                                            .Files
                                                                            .Select(x =>
                                                                                 PackageHelper
                                                                                    .ToPurl(CurseForgeService
                                                                                            .LABEL,
                                                                                         null,
                                                                                         x.ProjectId
                                                                                            .ToString(),
                                                                                         x.FileId
                                                                                            .ToString()))
                                                                            .ToList(),
                                                                         new List<string>(),
                                                                         new List<string>()),
                                                        new Dictionary<string, object>()),
                                            pack
                                               .FileNames
                                               .Where(x => x.StartsWith(manifest.Overrides)
                                                        && x != manifest.Overrides
                                                        && x.Length > manifest.Overrides.Length + 1)
                                               .Select(x => (x, x[(manifest.Overrides.Length + 1)..]))
                                               .ToList(),
                                            pack.Reference?.Thumbnail);
    }

    #endregion
}