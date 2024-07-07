using Microsoft.Extensions.Logging;
using NanoidDotNet;
using Polymerium.Trident.Services.Extracting;
using System.IO.Compression;
using Trident.Abstractions;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Extractors;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Services;

public class ModpackExtractor(
    ILogger<ModpackExtractor> logger,
    IEnumerable<IExtractor> extractors,
    ProfileManager profileManager,
    StorageManager storageManager,
    ThumbnailSaver thumbnailSaver)
{
    public async Task<FlattenExtractedContainer> ExtractAsync(Stream stream,
        (Project, Project.Version)? source, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        try
        {
            using ZipArchive archive = new(stream, ZipArchiveMode.Read, true);
            var fileNames = archive.Entries.Select(x => x.FullName).ToArray();
            var extractor = extractors.FirstOrDefault(x => fileNames.Contains(x.IdenticalFileName));
            if (extractor != null)
            {
                logger.LogInformation("Modpack in stream matches {label} extractor", extractor.GetType().Name);
                ExtractorContext context = new(source);
                var manifestEntry = archive.GetEntry(extractor.IdenticalFileName)!;
                await using var manifestStream = manifestEntry.Open();
                using StreamReader manifestReader = new(manifestStream);
                var manifestContent = await manifestReader.ReadToEndAsync(token);
                var extracted =
                    await extractor.ExtractAsync(manifestContent, context, token);
                var flatten =
                    FlattenExtractedContainer.FromExtracted(extracted, archive, source);
                return flatten;
            }

            logger.LogInformation("Modpack found no extractor matched");
            throw new NotSupportedException();
        }
        catch (InvalidDataException e)
        {
            logger.LogInformation("Passing stream contains no valid zip data");
            throw new BadFormatException("<stream>", e);
        }
        catch (Exception e)
        {
            logger.LogError("Unknown exception occurred while processing: {message}", e.Message);
            throw;
        }
    }

    public async Task SolidifyAsync(FlattenExtractedContainer container, string? nameOverride)
    {
        var name = nameOverride ?? container.Original.Name;
        var key = profileManager.RequestKey(name);
        List<Metadata.Layer> layers = new();

        var thumbnail = container.Reference?.Item1.Thumbnail;
        var reference = container.Reference.HasValue
            ? new Attachment(container.Reference.Value.Item1.Label, container.Reference.Value.Item1.Id,
                container.Reference.Value.Item2.Id)
            : null;

        logger.LogInformation("Modpack {name} requested a key {key}", name, key.Key);
        Metadata metadata = new(container.Original.Version, layers);
        foreach (var item in container.Layers)
        {
            List<Loader> loaders = new();

            loaders.AddRange(item.Original.Loaders);

            if (item.SolidFiles.Any())
            {
                var id = await Nanoid.GenerateAsync(size: 12);
                var storageKey = storageManager.RequestKey($"{key.Key}.{id}");
                var storage = storageManager.Open(storageKey);
                storage.EnsureEmpty();
                foreach (var file in item.SolidFiles)
                {
                    await storage.WriteAsync(file.FileName, file.Data.ToArray());
                }

                Loader storageLoader = new(Loader.COMPONENT_BUILTIN_STORAGE, storageKey);
                loaders.Add(storageLoader);
            }

            var attachments = item.Original.Attachments;
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