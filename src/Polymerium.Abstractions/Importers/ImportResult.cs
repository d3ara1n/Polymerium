using System.Collections.Generic;
using System.IO.Compression;

namespace Polymerium.Abstractions.Importers;

public class ImportResult
{
    public ImportResult(
        ZipArchive archive,
        GameInstance instance,
        IEnumerable<PackedSolidFile> files
    )
    {
        Archive = archive;
        Instance = instance;
        Files = files;
    }

    public ZipArchive Archive { get; set; }
    public GameInstance Instance { get; set; }
    public IEnumerable<PackedSolidFile> Files { get; set; }
}
