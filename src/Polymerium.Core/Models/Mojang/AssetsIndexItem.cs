using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Mojang
{
    public struct AssetsIndexItem
    {
        public string FileName { get; set; }
        public string Hash { get; set; }
        public uint Size { get; set; }
    }
}
