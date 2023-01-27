using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Mojang.Indexes
{
    public struct LibraryDownloadsClassifier
    {
        public string Identity { get; set; }
        public string Path { get; set; }
        public string Sha1 { get; set; }
        public Uri Url { get; set; }
        public uint Size { get; set; }
    }
}
