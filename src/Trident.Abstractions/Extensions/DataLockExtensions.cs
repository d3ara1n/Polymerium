﻿using Trident.Abstractions.FileModels;

namespace Trident.Abstractions.Extensions;

public static class DataLockExtensions
{
    public static bool Verify(this DataLock self, string key, Profile.Rice setup)
    {
        if (self.Viability.Format != DataLock.FORMAT)
            return false;

        if (self.Viability.Home != PathDef.Default.Home
         || self.Viability.Key != key
         || self.Viability.Version != setup.Version
         || self.Viability.Loader != setup.Loader)
            return false;

        if (self.Viability.Packages.Count != setup.Stage.Count + setup.Stash.Count)
            return false;

        var map = self.Viability.Packages.Distinct().ToDictionary(x => x, _ => 0);

        foreach (var check in setup.Stage.Concat(setup.Stash))
        {
            if (!map.TryGetValue(check, out var value))
                return false;

            map[check] = ++value;
        }

        return map.Values.All(x => x == 1);
    }
}