using System.IO.Compression;
using DotNext;
using Microsoft.Extensions.Logging;
using NanoidDotNet;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services.Extracting;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Extractors;
using Trident.Abstractions.Profiles;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Services;

public class ModpackExtractor(
    ILogger<ModpackExtractor> logger,
    IEnumerable<IExtractor> extractors,
    ProfileManager profileManager,
    StorageManager storageManager)
{
    public async Task<Result<FlattenExtractedContainer, ExtractError>> ExtractAsync(Stream stream,
        (Project, Project.Version)? source, CancellationToken token = default)
    {
        if (token.IsCancellationRequested)
            return new Result<FlattenExtractedContainer, ExtractError>(ExtractError.Cancelled);
        try
        {
            var archive = new ZipArchive(stream, ZipArchiveMode.Read, true);
            var fileNames = archive.Entries.Select(x => x.FullName).ToArray();
            var extractor = extractors.FirstOrDefault(x => fileNames.Contains(x.IdenticalFileName));
            if (extractor != null)
            {
                logger.LogInformation("Modpack in stream matches {label} extractor", extractor.GetType().Name);
                var context = new ExtractorContext(source);
                var manifestEntry = archive.GetEntry(extractor.IdenticalFileName)!;
                await using var manifestStream = manifestEntry.Open();
                using var manifestReader = new StreamReader(manifestStream);
                var manifestContent = await manifestReader.ReadToEndAsync(token);
                var result = await extractor.ExtractAsync(manifestContent, context, token);
                if (result.IsSuccessful)
                {
                    var extracted = result.Value;
                    var flatten = FlattenExtractedContainer.FromExtracted(extracted, archive, source);
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
        var name = nameOverride ?? container.Original.Name;
        var key = profileManager.RequestKey(name);
        var layers = new List<Metadata.Layer>();

        logger.LogInformation("Modpack {name} requested a key {key}", name, key.Key);
        var metadata = new Metadata(container.Original.Version, layers);
        foreach (var item in container.Layers)
        {
            var loaders = new List<Loader>();

            loaders.AddRange(item.Original.Loaders);

            if (item.SolidFiles.Any())
            {
                var id = await Nanoid.GenerateAsync(size: 12);
                var storageKey = storageManager.RequestKey($"{key.Key}.{id}");
                var storage = storageManager.Open(storageKey);
                storage.EnsureEmpty();
                foreach (var file in item.SolidFiles)
                    await storage.WriteAsync(file.FileName, file.Data.ToArray());
                var storageLoader = new Loader(Loader.COMPONENT_BUILTIN_STORAGE, storageKey);
                loaders.Add(storageLoader);
            }

            var attachments = item.Original.Attachments
                .Select(x => PurlHelper.MakePurl(x.Label, x.ProjectId, x.VersionId)).ToList();
            var layer = new Metadata.Layer(null, true, item.Original.Summary, loaders, attachments);
            layers.Add(layer);
        }

        var thumbnail = container.Reference?.Item1.Thumbnail;
        var reference = container.Reference.HasValue
            ? PurlHelper.MakePurl(container.Reference.Value.Item1.Label, container.Reference.Value.Item1.Id,
                container.Reference.Value.Item2.Id)
            : null;
        logger.LogInformation(
            "Modpack {name}({key}) metadata generated, ready to be appended: {version} versioned, {layers} layers",
            name, key.Key, metadata.Version, metadata.Layers.Count);
        profileManager.Append(key, name, thumbnail, reference, metadata);
    }
}