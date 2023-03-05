using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNext;

namespace Polymerium.Abstractions.Importers;

public abstract class ImporterBase
{
    public CancellationToken Token { get; set; }
    public abstract Task<Result<ImportResult, GameImportError>> ProcessAsync(ZipArchive archive);

    public Result<ImportResult, GameImportError> Failed(GameImportError reason)
    {
        return new Result<ImportResult, GameImportError>(reason);
    }

    public Result<ImportResult, GameImportError> Finished(
        ZipArchive archive,
        GameInstance instance,
        IEnumerable<PackedSolidFile>? files = null
    )
    {
        return new ImportResult(archive, instance, files ?? Enumerable.Empty<PackedSolidFile>());
    }
}