using System.IO.Compression;
using DotNext;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Services.Extracting;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Extractors;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Services;

public class ModpackExtractor(ILogger<ModpackExtractor> logger, IEnumerable<IExtractor> extractors)
{
    public async Task<Result<FlattenExtractedContainer, ExtractError>> ExtractAsync(string path,
        (Project, Project.Version)? source, CancellationToken token = default)
    {
        if (token.IsCancellationRequested)
            return new Result<FlattenExtractedContainer, ExtractError>(ExtractError.Cancelled);
        try
        {
            await using var stream = File.OpenRead(path);
            var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var fileNames = archive.Entries.Select(x => x.FullName).ToArray();
            var extractor = extractors.FirstOrDefault(x => fileNames.Contains(x.IdenticalFileName));
            if (extractor != null)
            {
                logger.LogInformation("File {modpack} matches {label} extractor", Path.GetFileName(path),
                    extractor.GetType().Name);
                var context = new ExtractorContext(source);
                var manifestEntry = archive.GetEntry(extractor.IdenticalFileName)!;
                await using var manifestStream = manifestEntry.Open();
                using var manifestReader = new StreamReader(manifestStream);
                var manifestContent = manifestReader.ReadToEnd();
                var result = await extractor.ExtractAsync(manifestContent, context, token);
                if (result.IsSuccessful)
                {
                    var extracted = result.Value;
                    var flatten = FlattenExtractedContainer.FromExtracted(extracted, archive);
                    archive.Dispose();
                    return new Result<FlattenExtractedContainer, ExtractError>(flatten);
                }

                logger.LogInformation("File {modpack} found no extractor matched", Path.GetFileName(path));
                return new Result<FlattenExtractedContainer, ExtractError>(result.Error);
            }

            return new Result<FlattenExtractedContainer, ExtractError>(ExtractError.Unsupported);
        }
        catch (FileNotFoundException)
        {
            logger.LogInformation("Passing modpack path does not exist: {path}", path);
            return new Result<FlattenExtractedContainer, ExtractError>(ExtractError.FileNotFound);
        }
        catch (Exception e)
        {
            logger.LogError("Unknown exception occurred while processing {path}: {message}", path, e.Message);
            return new Result<FlattenExtractedContainer, ExtractError>(ExtractError.Exception);
        }
    }
}