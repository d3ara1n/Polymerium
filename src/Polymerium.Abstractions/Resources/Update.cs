using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.Resources
{
    public record Update : ResourceBase
    {
        public Update(string id, string name, string version, string versionId)
            : base(id, name, version, string.Empty, null, null, string.Empty, versionId, null, null)
        {
            throw new NotImplementedException();
        }
    }
}
