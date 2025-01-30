namespace Trident.Abstractions.Importers;

public interface IProfileImporter
{
    string IndexFileName { get; }

    Task<ImportedProfileContainer> ExtractAsync(CompressedProfilePack pack);
}