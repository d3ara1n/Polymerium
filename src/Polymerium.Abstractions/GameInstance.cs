using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions.Meta;

namespace Polymerium.Abstractions
{
    public class GameInstance
    {
        public string Id { get; set; }
        public GameMetadata Metadata { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string FolderName { get; set; }
        public string ThumbnailFile { get; set; }
        public string BoundAccountId { get; set; }
    }
}
