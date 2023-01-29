using System;
using System.Collections.Generic;

namespace Polymerium.Abstractions.Meta
{
    public struct GameMetadata
    {
        public string CoreVersion { get; set; }
        public string StructureSha1 { get; set; }
        public IEnumerable<Component> Components { get; set; }
        public IEnumerable<Uri> Attachments { get; set; }
    }
}