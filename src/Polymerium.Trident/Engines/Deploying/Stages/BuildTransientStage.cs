using Microsoft.Extensions.Logging;
using Polymerium.Trident.Models.Minecraft;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Trident.Abstractions.Building;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Engines.Deploying.Stages
{
    public class BuildTransientStage(IHttpClientFactory factory) : StageBase
    {
        protected override async Task OnProcessAsync()
        {
            // transfer basic data from artifact
            Artifact artifact = Context.Artifact!;
            TransientData transient = new();

            string indexFile = Context.Trident.AssetIndexPath(artifact.AssetIndex.Id);
            transient.AddPresent(
                new TransientData.PresentFile(indexFile, artifact.AssetIndex.Url, artifact.AssetIndex.Sha1));

            MinecraftAssetIndex index =
                await GetAssetIndexAsync(indexFile, artifact.AssetIndex.Url, artifact.AssetIndex.Sha1);
            foreach (MinecraftAssetIndexObject obj in index.Objects.Values)
            {
                string path = Context.Trident.AssetObjectPath(obj.Hash);
                transient.AddPresent(new TransientData.PresentFile(path,
                    new Uri($"https://resources.download.minecraft.net/{obj.Hash[..2]}/{obj.Hash}", UriKind.Absolute)
                    , obj.Hash));
            }

            foreach (Artifact.Parcel parcel in artifact.Parcels)
            {
                transient.AddFragile(new TransientData.FragileFile(
                    Path.Combine(Context.Trident.ObjectDir, parcel.SourcePath),
                    Path.Combine(Context.Trident.InstanceHomePath(Context.Key), parcel.TargetPath), parcel.Url,
                    parcel.Sha1));
            }

            string nativesDir = Context.Trident.NativeDirPath(Context.Key);

            foreach (Artifact.Library library in artifact.Libraries)
            {
                string libraryPath = Context.Trident.LibraryPath(library.Id.Namespace, library.Id.Name,
                    library.Id.Version,
                    library.Id.Platform, library.Id.Extension);
                transient.AddPresent(
                    new TransientData.PresentFile(Path.Combine(libraryPath), library.Url, library.Sha1));
                if (library.IsNative)
                {
                    transient.AddExplosive(new TransientData.ExplosiveFile(libraryPath, nativesDir));
                }
            }

            ICollection<string> bag = BuildKeywords(Context.Keywords);
            Logger.LogInformation("Run processors with keywords bag: [{bag}]", string.Join(',', bag));
            Artifact.Processor[] processors = Context.Artifact!.Processors.ToArray();
            foreach (Artifact.Processor processor in processors)
            {
                bool check = CheckCondition(processor.Condition, bag);
                if (check)
                {
                    Logger.LogInformation("Run processor: {id}", processor.Action);

                    switch (processor.Action)
                    {
                        case TransientData.PROCESSOR_TRIDENT_STORAGE:
                            string storageHome =
                                Path.Combine(Context.Trident.StorageDir, processor.Data ?? string.Empty);
                            if (Directory.Exists(storageHome))
                            {
                                List<string> files = new();
                                FillAllFilesInDirectory(files, storageHome);
                                string instanceHome = Context.Trident.InstanceHomePath(Context.Key);
                                foreach (string file in files)
                                {
                                    transient.AddPersistent(new TransientData.PersistentFile(file,
                                        Path.Combine(instanceHome, Path.GetRelativePath(storageHome, file))));
                                }
                            }

                            break;
                        default:
                            throw new ResourceIdentityUnrecognizedException(processor.Action,
                                nameof(Artifact.Processor));
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
                string[] directories = Directory.GetDirectories(dir);
                foreach (string directory in directories)
                {
                    FillAllFilesInDirectory(container, Path.Combine(dir, directory));
                }

                string[] files = Directory.GetFiles(dir);
                foreach (string file in files)
                {
                    container.Add(file);
                }
            }
        }

        private async ValueTask<MinecraftAssetIndex> GetAssetIndexAsync(string indexFile, Uri url, string hash)
        {
            if (File.Exists(indexFile))
            {
                await using FileStream reader = File.OpenRead(indexFile);
                string computed = BitConverter.ToString(await SHA1.HashDataAsync(reader)).Replace("-", string.Empty)
                    .ToLower();
                reader.Position = 0;
                if (computed == hash)
                {
                    return await JsonSerializer.DeserializeAsync<MinecraftAssetIndex>(reader,
                        Context.SerializerOptions);
                }
            }

            using HttpClient client = factory.CreateClient();
            return await client.GetFromJsonAsync<MinecraftAssetIndex>(url, Context.SerializerOptions);
        }

        private ICollection<string> BuildKeywords(ICollection<string> original)
        {
            List<string> bag = new(original);
            foreach (Loader loader in Context.Metadata.Layers.Where(x => x.Enabled).SelectMany(x => x.Loaders))
            {
                string keyword = $"component:{loader.Id}";
                if (!bag.Contains(keyword))
                {
                    bag.Add(keyword);
                }
            }

            return bag;
        }

        private bool CheckCondition(string? condition, ICollection<string> bag)
        {
            if (condition == null)
            {
                return true;
            }

            bool result = false;
            IEnumerable<string> subs = condition.Split('|').Where(x => !string.IsNullOrEmpty(x));
            foreach (string sub in subs)
            {
                bool inner = true;
                IEnumerable<string> pieces = sub.Split('&').Where(x => !string.IsNullOrEmpty(x));
                foreach (string piece in pieces)
                {
                    inner &= piece.StartsWith('!') ? !bag.Contains(piece[1..]) : bag.Contains(piece);
                }

                result |= inner;
            }

            return result;
        }

        private void ProcessStorage()
        {
        }
    }
}