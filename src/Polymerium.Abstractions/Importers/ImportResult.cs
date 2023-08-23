using System;
using System.IO.Compression;

namespace Polymerium.Abstractions.Importers;

public class ImportResult: IDisposable
{
    public ImportResult(ZipArchive archive, ModpackContent content)
    {
        Archive = archive;
        Content = content;
    }

    public ZipArchive Archive { get; set; }
    public ModpackContent Content { get; set; }

    public void Dispose()
    {
        Archive.Dispose();
    }
}
