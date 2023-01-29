using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Core.Models.Mojang;

namespace Polymerium.Core.ResourceResolving
{
    //[Domain("mojang)]
    public class MojangResolver : ResourceResolverBase
    {
        // poly-res://core:mojang/versions
        //[Resource(Domain="mojang",Type="core",Expression="versions")]
        public VersionManifest? GetVersions()
        {
            return null;
        }
    }
}