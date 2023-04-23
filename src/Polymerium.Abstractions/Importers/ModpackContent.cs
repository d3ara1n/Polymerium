using Polymerium.Abstractions.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.Importers
{
    public struct ModpackContent
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public Uri? ThumbnailFile { get; set; }
        public Uri? ReferenceSource { get; set; }
        public GameMetadata Metadata { get; set; }
        public IEnumerable<PackedSolidFile> Files { get; set; }
    }
}
