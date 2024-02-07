using Trident.Abstractions;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Extensions;

public static class MetadataExtensions
{
    public static Filter ExtractFilter(this Metadata self, ResourceKind? kind = null)
    {
        return new Filter(self.Version,
            self.ExtractModLoader(), kind);
    }
}