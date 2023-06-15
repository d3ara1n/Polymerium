using DotNext;
using Newtonsoft.Json;
using Polymerium.Abstractions.Importers;
using Polymerium.Abstractions.Meta;
using Polymerium.Core.Models.Prism;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Importers
{
    public class PrismImporter : ImporterBase
    {
        public override Task<Result<ModpackContent, GameImportError>> ExtractMetadataAsync(string fileName, string indexContent, IEnumerable<string> rawFileList, Uri? source, bool forceOffline)
        {
            var model = JsonConvert.DeserializeObject<PrismModpackIndex>(indexContent);
            var metadata = new GameMetadata();
            foreach (var component in model.Components)
            {
                metadata.Components.Add(new Component()
                {
                    Identity = component.Uid,
                    Version = component.Version
                });
            }
            var list = new List<PackedSolidFile>();
            foreach (var raw in rawFileList.Where(x => x.StartsWith("minecraft")))
            {
                var packed = new PackedSolidFile()
                {
                    FileName = raw,
                    Path = Path.GetRelativePath("minecraft", raw)
                };
                list.Add(packed);
            }
            foreach (var raw in rawFileList.Where(x => x.StartsWith(".minecraft")))
            {
                var packed = new PackedSolidFile()
                {
                    FileName = raw,
                    Path = Path.GetRelativePath(".minecraft", raw)
                };
                list.Add(packed);
            }
            var name = Path.GetFileNameWithoutExtension(fileName);
            var version = Path.GetFileName(fileName);
            return Task.FromResult(Finished(name, version, string.Empty, null, null, metadata, list));
        }
    }
}
