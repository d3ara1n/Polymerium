using Trident.Abstractions.Importers;

namespace Polymerium.Trident.Services;

public class ImporterAgent(IEnumerable<IProfileImporter> importers)
{
    public async Task<ImportedProfileContainer> ImportAsync(CompressedProfilePack pack)
    {
        var importer = importers.FirstOrDefault(x => pack.FileNames.Contains(x.IndexFileName));
        if (importer is not null) return await importer.ExtractAsync(pack);

        throw new ImporterNotFoundException();
    }
}