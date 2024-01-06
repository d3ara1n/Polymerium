using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trident.Abstractions.Resources
{
    public record Requirement(IEnumerable<string> AnyOfVersions, IEnumerable<string> AnyOfLoaders);
}
