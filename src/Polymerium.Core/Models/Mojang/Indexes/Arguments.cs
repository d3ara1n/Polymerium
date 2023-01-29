using System.Collections.Generic;

namespace Polymerium.Core.Models.Mojang.Indexes
{
    public struct Arguments
    {
        public IEnumerable<ArgumentsItem> Game { get; set; }
        public IEnumerable<ArgumentsItem> Jvm { get; set; }
    }
}