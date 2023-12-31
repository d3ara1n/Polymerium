using Trident.Abstractions.Resources;

namespace Trident.Abstractions.Repositories
{
    public record Filter(string? Version, string? ModLoader, ResourceKind? Kind)
    {
        public static readonly Filter EMPTY = new(null, null, null);
    }
}
