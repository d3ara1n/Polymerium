using System.IO.Compression;
using Trident.Abstractions.Repositories.Resources;

namespace Trident.Abstractions.Importers;

public class CompressedProfilePack
{
    private readonly ZipArchive _archive;

    // input should be MemoryStream in practice
    public CompressedProfilePack(Stream input)
    {
        _archive = new ZipArchive(input, ZipArchiveMode.Read, true);
        FileNames = [.. _archive.Entries.Select(x => x.FullName)];
    }

    public IReadOnlyList<string> FileNames { get; }
    public Package? Reference { get; set; }

    public Stream Open(string fileName) => _archive.GetEntry(fileName)!.Open();
}