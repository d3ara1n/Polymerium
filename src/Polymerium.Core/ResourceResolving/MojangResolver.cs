using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions.ResourceResolving;

namespace Polymerium.Core.ResourceResolving
{
    //[Domain("mojang)]
    public class MojangResolver : IResourceResolver
    {
        //[UrlExtract("{version}")]
        //[UrlLabel(ResourceType.Core)]
        public void GetVersion(string version)
        {
            //
        }
    }
}
