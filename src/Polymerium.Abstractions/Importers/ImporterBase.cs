using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.Importers;

public abstract class ImporterBase
{
    public abstract Task<Result<ImportResult, GameImportError>> ProcessAsync(ZipArchive archive);

    public Result<ImportResult, GameImportError> Failed(GameImportError reason)
    {
        return Result<ImportResult, GameImportError>.Err(reason);
    }

    public Result<ImportResult, GameImportError> Finished(
        ZipArchive archive,
        GameInstance instance,
        IEnumerable<PackedSolidFile>? files = null
    )
    {
        return Result<ImportResult, GameImportError>.Ok(
            new ImportResult(archive, instance, files ?? Enumerable.Empty<PackedSolidFile>())
        );
    }
}
