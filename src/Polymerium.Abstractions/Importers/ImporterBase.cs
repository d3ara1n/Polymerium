using DotNext;
using Polymerium.Abstractions.Meta;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.Importers;

public abstract class ImporterBase
{
    public CancellationToken Token { get; set; }

    public abstract Task<Result<ModpackContent, GameImportError>> ExtractMetadataAsync(
        string fileName,
        string indexContent,
        IEnumerable<string> rawFileList,
        Uri? source,
        bool forceOffline
    );

    protected Result<ModpackContent, GameImportError> Failed(GameImportError reason)
    {
        return new Result<ModpackContent, GameImportError>(reason);
    }

    protected Result<ModpackContent, GameImportError> Finished(
        string name,
        string version,
        string author,
        Uri? thumbnail,
        Uri? reference,
        GameMetadata metadata,
        IEnumerable<PackedSolidFile> files
    )
    {
        return new ModpackContent
        {
            Name = name,
            Version = version,
            Author = author,
            ThumbnailFile = thumbnail,
            ReferenceSource = reference,
            Metadata = metadata,
            Files = files
        };
    }
}
