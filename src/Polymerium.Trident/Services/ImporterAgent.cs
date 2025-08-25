using Trident.Abstractions;
using Trident.Abstractions.Importers;

namespace Polymerium.Trident.Services
{
    public class ImporterAgent(IEnumerable<IProfileImporter> importers)
    {
        public static readonly string[] INVALID_NAMES = ["", ".", ".."];

        public async Task<ImportedProfileContainer> ImportAsync(CompressedProfilePack pack)
        {
            var importer = importers.FirstOrDefault(x => pack.FileNames.Contains(x.IndexFileName));
            if (importer is not null)
            {
                return await importer.ExtractAsync(pack).ConfigureAwait(false);
            }

            throw new ImporterNotFoundException();
        }

        public async Task ExtractImportFilesAsync(
            string key,
            ImportedProfileContainer container,
            CompressedProfilePack pack)
        {
            var importDir = PathDef.Default.DirectoryOfImport(key);

            foreach (var (source, target) in container.ImportFileNames)
            {
                var to = Path.Combine(importDir, target);
                var dir = Path.GetDirectoryName(to);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var fromStream = pack.Open(source);
                var file = new FileStream(to, FileMode.Create);
                await fromStream.CopyToAsync(file).ConfigureAwait(false);
                await file.FlushAsync().ConfigureAwait(false);
                file.Close();
                fromStream.Close();
            }
        }
    }
}
