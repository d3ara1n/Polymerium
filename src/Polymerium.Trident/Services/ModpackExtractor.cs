using DotNext;
using Microsoft.Extensions.Logging;
using NanoidDotNet;
using Polymerium.Trident.Services.Extracting;
using Polymerium.Trident.Services.Profiles;
using Polymerium.Trident.Services.Storages;
using System.IO.Compression;
using Trident.Abstractions;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Extractors;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Services
{
    public class ModpackExtractor(
        ILogger<ModpackExtractor> logger,
        IEnumerable<IExtractor> extractors,
        ProfileManager profileManager,
        StorageManager storageManager,
        ThumbnailSaver thumbnailSaver)
    {
        public async Task<Result<FlattenExtractedContainer, ExtractError>> ExtractAsync(Stream stream,
            (Project, Project.Version)? source, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return new Result<FlattenExtractedContainer, ExtractError>(ExtractError.Cancelled);
            }

            try
            {
                ZipArchive archive = new(stream, ZipArchiveMode.Read, true);
                string[] fileNames = archive.Entries.Select(x => x.FullName).ToArray();
                IExtractor? extractor = extractors.FirstOrDefault(x => fileNames.Contains(x.IdenticalFileName));
                if (extractor != null)
                {
                    logger.LogInformation("Modpack in stream matches {label} extractor", extractor.GetType().Name);
                    ExtractorContext context = new(source);
                    ZipArchiveEntry manifestEntry = archive.GetEntry(extractor.IdenticalFileName)!;
                    await using Stream manifestStream = manifestEntry.Open();
                    using StreamReader manifestReader = new(manifestStream);
                    string manifestContent = await manifestReader.ReadToEndAsync(token);
                    Result<ExtractedContainer, ExtractError> result =
                        await extractor.ExtractAsync(manifestContent, context, token);
                    if (result.IsSuccessful)
                    {
                        ExtractedContainer extracted = result.Value;
                        FlattenExtractedContainer flatten =
                            FlattenExtractedContainer.FromExtracted(extracted, archive, source);
                        archive.Dispose();
                        return new Result<FlattenExtractedContainer, ExtractError>(flatten);
                    }

                    archive.Dispose();
                    logger.LogInformation("Modpack found no extractor matched");
                    return new Result<FlattenExtractedContainer, ExtractError>(result.Error);
                }

                return new Result<FlattenExtractedContainer, ExtractError>(ExtractError.Unsupported);
            }
            catch (InvalidDataException)
            {
                logger.LogInformation("Passing stream contains no valid zip data");
                return new Result<FlattenExtractedContainer, ExtractError>(ExtractError.FileNotFound);
            }
            catch (Exception e)
            {
                logger.LogError("Unknown exception occurred while processing: {message}", e.Message);
                return new Result<FlattenExtractedContainer, ExtractError>(ExtractError.Exception);
            }
        }

        public async Task SolidifyAsync(FlattenExtractedContainer container, string? nameOverride)
        {
            string name = nameOverride ?? container.Original.Name;
            ReservedKey key = profileManager.RequestKey(name);
            List<Metadata.Layer> layers = new();

            Uri? thumbnail = container.Reference?.Item1.Thumbnail;
            Attachment? reference = container.Reference.HasValue
                ? new Attachment(container.Reference.Value.Item1.Label, container.Reference.Value.Item1.Id,
                    container.Reference.Value.Item2.Id)
                : null;

            logger.LogInformation("Modpack {name} requested a key {key}", name, key.Key);
            Metadata metadata = new(container.Original.Version, layers);
            foreach (FlattenContainedLayer item in container.Layers)
            {
                List<Loader> loaders = new();

                loaders.AddRange(item.Original.Loaders);

                if (item.SolidFiles.Any())
                {
                    string? id = await Nanoid.GenerateAsync(size: 12);
                    string storageKey = storageManager.RequestKey($"{key.Key}.{id}");
                    Storage storage = storageManager.Open(storageKey);
                    storage.EnsureEmpty();
                    foreach (SolidFile file in item.SolidFiles)
                    {
                        await storage.WriteAsync(file.FileName, file.Data.ToArray());
                    }

                    Loader storageLoader = new(Loader.COMPONENT_BUILTIN_STORAGE, storageKey);
                    loaders.Add(storageLoader);
                }

                IList<Attachment> attachments = item.Original.Attachments;
                Metadata.Layer layer = new(reference, true, item.Original.Summary, loaders, attachments);
                layers.Add(layer);
            }

            logger.LogInformation(
                "Modpack {name}({key}) metadata generated, ready to be appended: {version} versioned, {layers} layers",
                name, key.Key, metadata.Version, metadata.Layers.Count);
            if (thumbnail != null)
            {
                await thumbnailSaver.SaveAsync(key.Key, thumbnail);
            }

            profileManager.Append(key, name, reference, metadata);
        }
    }
}