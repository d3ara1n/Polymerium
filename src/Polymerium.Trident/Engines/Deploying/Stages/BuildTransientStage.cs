using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Models.Minecraft;
using Trident.Abstractions.Building;
using Trident.Abstractions.Exceptions;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class BuildTransientStage(IHttpClientFactory factory) : StageBase
{
    protected override async Task OnProcessAsync()
    {
        // transfer basic data from artifact
        var artifact = Context.Artifact!;
        var transient = new TransientData();

        foreach (var parcel in artifact.Parcels)
            transient.AddFragile(new TransientData.FragileFile(
                Path.Combine(Context.Context.ObjectDir, parcel.SourcePath),
                Path.Combine(Context.Context.InstanceHomePath(Context.Key), parcel.TargetPath), parcel.Url,
                parcel.Sha1));

        foreach (var library in artifact.Libraries)
            transient.AddPresent(new TransientData.PresentFile(Path.Combine(
                Context.Context.LibraryPath(library.Id.Namespace, library.Id.Name, library.Id.Version,
                    library.Id.Platform)), library.Url, library.Sha1));

        var indexFile = Context.Context.AssetIndexPath(artifact.AssetIndex.Id);
        transient.AddPresent(
            new TransientData.PresentFile(indexFile, artifact.AssetIndex.Url, artifact.AssetIndex.Sha1));

        var index = await GetAssetIndexAsync(indexFile, artifact.AssetIndex.Url, artifact.AssetIndex.Sha1);
        foreach (var obj in index.Objects.Values)
        {
            var path = Context.Context.AssetObjectPath(obj.Hash);
            transient.AddPresent(new TransientData.PresentFile(path,
                new Uri($"https://resources.download.minecraft.net/{obj.Hash[..2]}/{obj.Hash}", UriKind.Absolute)
                , obj.Hash));
        }

        var bag = BuildKeywords(Context.Keywords);
        Logger.LogInformation("Run processors with keywords bag: [{bag}]", string.Join(',', bag));
        var processors = Context.Artifact!.Processors.ToArray();
        foreach (var processor in processors)
        {
            var check = CheckCondition(processor.Condition, bag);
            if (check)
            {
                Logger.LogInformation("Run processor: {id}", processor.Action);

                switch (processor.Action)
                {
                    case TransientData.PROCESSOR_TRIDENT_STORAGE:
                        var storageHome = Path.Combine(Context.Context.StorageDir, processor.Data ?? string.Empty);
                        if (Directory.Exists(storageHome))
                        {
                            var files = new List<string>();
                            FillAllFilesInDirectory(files, storageHome);
                            var instanceHome = Context.Context.InstanceHomePath(Context.Key);
                            foreach (var file in files)
                                transient.AddPersistent(new TransientData.PersistentFile(file,
                                    Path.Combine(instanceHome, Path.GetRelativePath(storageHome, file))));
                        }

                        break;
                    default:
                        throw new ResourceIdentityUnrecognizedException(processor.Action, nameof(Artifact.Processor));
                }
            }
            else
            {
                Logger.LogInformation("Skip processor: {id}", processor.Action);
            }
        }

        Context.Transient = transient;
    }

    private void FillAllFilesInDirectory(IList<string> container, string dir)
    {
        if (Directory.Exists(dir))
        {
            var directories = Directory.GetDirectories(dir);
            foreach (var directory in directories)
                FillAllFilesInDirectory(container, Path.Combine(dir, directory));
            var files = Directory.GetFiles(dir);
            foreach (var file in files)
                container.Add(file);
        }
    }

    private async ValueTask<MinecraftAssetIndex> GetAssetIndexAsync(string indexFile, Uri url, string hash)
    {
        if (File.Exists(indexFile))
        {
            await using var reader = File.OpenRead(indexFile);
            var computed = BitConverter.ToString(await SHA1.HashDataAsync(reader)).Replace("-", string.Empty).ToLower();
            reader.Position = 0;
            if (computed == hash)
                return await JsonSerializer.DeserializeAsync<MinecraftAssetIndex>(reader, Context.SerializerOptions);
        }

        using var client = factory.CreateClient();
        return await client.GetFromJsonAsync<MinecraftAssetIndex>(url, Context.SerializerOptions);
    }

    private ICollection<string> BuildKeywords(ICollection<string> original)
    {
        var bag = new List<string>(original);
        foreach (var loader in Context.Metadata.Layers.Where(x => x.Enabled).SelectMany(x => x.Loaders))
        {
            var keyword = $"component:{loader.Id}";
            if (!bag.Contains(keyword))
                bag.Add(keyword);
        }

        return bag;
    }

    private bool CheckCondition(string? condition, ICollection<string> bag)
    {
        if (condition == null) return true;

        var result = false;
        var subs = condition.Split('|').Where(x => !string.IsNullOrEmpty(x));
        foreach (var sub in subs)
        {
            var inner = true;
            var pieces = sub.Split('&').Where(x => !string.IsNullOrEmpty(x));
            foreach (var piece in pieces)
                inner &= piece.StartsWith('!') ? !bag.Contains(piece[1..]) : bag.Contains(piece);

            result |= inner;
        }

        return result;
    }

    private async Task ProcessStorageAsync()
    {
    }
}