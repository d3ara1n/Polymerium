using Trident.Abstractions.Importers;

namespace Polymerium.Trident.Importers;

public class ModrinthImporter : IProfileImporter
{
    public string IndexFileName => "modrinth.index.json";

    public Task<ImportedProfileContainer> ExtractAsync(CompressedProfilePack pack) =>
        throw new NotImplementedException();
}