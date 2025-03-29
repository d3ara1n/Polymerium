using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Trident.Abstractions;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class GenerateManifestStage(IHttpClientFactory factory) : StageBase
{
    protected override async Task OnProcessAsync(CancellationToken token)
    {
        var manifest = new EntityManifest();

        var artifact = Context.Artifact!;

        var indexPath = PathDef.Default.FileOfAssetIndex(artifact.AssetIndex.Id);
        manifest.PresentFiles.Add(new EntityManifest.PresentFile(indexPath,
                                                                 artifact.AssetIndex.Url,
                                                                 artifact.AssetIndex.Sha1));
        var index = await GetAssetIndexAsync(indexPath, artifact.AssetIndex.Url, artifact.AssetIndex.Sha1)
                       .ConfigureAwait(false)
                 ?? throw new
                        InvalidOperationException("Asset index file is broken or not matched with builtin models");
        foreach (var obj in index.Objects)
        {
            var path = PathDef.Default.FileOfAssetObject(obj.Value.Hash);
            manifest.PresentFiles.Add(new EntityManifest.PresentFile(path,
                                                                     new
                                                                         Uri($"https://resources.download.minecraft.net/{obj.Value.Hash[..2]}/{obj.Value.Hash}",
                                                                             UriKind.Absolute),
                                                                     obj.Value.Hash));
        }

        foreach (var parcel in artifact.Parcels)
            manifest.FragileFiles.Add(new EntityManifest.FragileFile(PathDef.Default.FileOfPackageObject(parcel.Label,
                                                                         parcel.Namespace,
                                                                         parcel.Pid,
                                                                         parcel.Vid),
                                                                     Path.Combine(PathDef.Default
                                                                            .DirectoryOfHome(Context.Key),
                                                                         parcel.Path),
                                                                     parcel.Download,
                                                                     parcel.Sha1));

        var nativesDir = PathDef.Default.DirectoryOfNatives(Context.Key);
        foreach (var lib in artifact.Libraries)
        {
            var path = PathDef.Default.FileOfLibrary(lib.Id.Namespace,
                                                     lib.Id.Name,
                                                     lib.Id.Version,
                                                     lib.Id.Platform,
                                                     lib.Id.Extension);
            manifest.PresentFiles.Add(new EntityManifest.PresentFile(path, lib.Url, lib.Sha1));
            if (lib.IsNative)
                manifest.ExplosiveFiles.Add(new EntityManifest.ExplosiveFile(path, nativesDir, false));
        }

        var importDir = PathDef.Default.DirectoryOfImport(Context.Key);

        if (Directory.Exists(importDir))
        {
            var buildDir = PathDef.Default.DirectoryOfBuild(Context.Key);
            var dirs = new Stack<string>();
            dirs.Push(importDir);

            while (dirs.TryPop(out var sub))
            {
                foreach (var file in Directory.GetFiles(sub))
                    manifest.PersistentFiles.Add(new EntityManifest.PersistentFile(file,
                                                                    Path.Combine(buildDir, Path.GetRelativePath(importDir, file)),
                                                                    false));
                foreach (var dir in Directory.GetDirectories(sub))
                    dirs.Push(dir);
            }
        }


        Context.Manifest = manifest;
    }

    private async ValueTask<MinecraftAssetIndex?> GetAssetIndexAsync(string indexFile, Uri url, string hash)
    {
        if (File.Exists(indexFile))
        {
            await using var reader = File.OpenRead(indexFile);
            var computed = Convert.ToHexStringLower(await SHA1.HashDataAsync(reader).ConfigureAwait(false));
            reader.Position = 0;
            if (computed == hash)
                return await JsonSerializer
                            .DeserializeAsync<MinecraftAssetIndex>(reader, JsonSerializerOptions.Web)
                            .ConfigureAwait(false);
        }

        using var client = factory.CreateClient();
        return await client.GetFromJsonAsync<MinecraftAssetIndex>(url, JsonSerializerOptions.Web).ConfigureAwait(false);
    }

    private record MinecraftAssetIndex(IDictionary<string, MinecraftAssetIndex.MinecraftAssetIndexObject> Objects)
    {
        public record MinecraftAssetIndexObject(string Hash, uint Size);
    }
}