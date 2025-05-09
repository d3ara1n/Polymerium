using Trident.Abstractions.FileModels;

namespace Trident.Abstractions.Extensions;

public static class DataLockExtensions
{
    public static bool Verify(this DataLock self, string key, Profile.Rice setup, string watermark)
    {
        if (self.Viability.Format != DataLock.FORMAT || self.Viability.Watermark != watermark)
            return false;

        if (self.Viability.Home != PathDef.Default.Home
         || self.Viability.Key != key
         || self.Viability.Version != setup.Version
         || self.Viability.Loader != setup.Loader)
            return false;

        if (self.Viability.Packages.Count != setup.Packages.Count(x => x.Enabled))
            return false;

        var map = self.Viability.Packages.Distinct().ToHashSet();

        var rv = map.SetEquals(setup.Packages.Where(x => x.Enabled).Select(x => x.Purl).Distinct());
        return rv;
    }
}