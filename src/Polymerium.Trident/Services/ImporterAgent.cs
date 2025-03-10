using Trident.Abstractions;
using Trident.Abstractions.Importers;

namespace Polymerium.Trident.Services;

public class ImporterAgent(IEnumerable<IProfileImporter> importers)
{
    private static readonly string[] INVALID_NAMES = ["", ".", ".."];

    public async Task<ImportedProfileContainer> ImportAsync(CompressedProfilePack pack)
    {
        var importer = importers.FirstOrDefault(x => pack.FileNames.Contains(x.IndexFileName));
        if (importer is not null)
            return await importer.ExtractAsync(pack);

        throw new ImporterNotFoundException();
    }

    public async Task ExtractImportFilesAsync(
        string key,
        ImportedProfileContainer container,
        CompressedProfilePack pack)
    {
        var importDir = PathDef.Default.DirectoryOfImport(key);

        foreach (var (source, target) in container.ImportFileNames.Where(x => !x.Item2.EndsWith('/')
                                                                           && !INVALID_NAMES.Contains(x.Item2)))
        {
            var to = Path.Combine(importDir, target);
            var dir = Path.GetDirectoryName(to);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var fromStream = pack.Open(source);
            var file = new FileStream(to, FileMode.Create);
            await fromStream.CopyToAsync(file);
            await file.FlushAsync();
            file.Close();
            fromStream.Close();
        }
    }
}