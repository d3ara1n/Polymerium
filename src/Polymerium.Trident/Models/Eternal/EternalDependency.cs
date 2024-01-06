using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Trident.Models.Eternal
{
    public struct EternalDependency
    {
        public uint ModId { get; set; }
        /// <summary>
        /// 1 = EmbeddedLibrary
        /// 2 = OptionalDependency
        /// 3 = RequiredDependency
        /// 4 = Tool
        /// 5 = Incompatible
        /// 6 = Include
        /// </summary>
        public uint RelationType { get; set; }
    }
}
