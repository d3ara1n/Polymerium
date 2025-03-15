using System.Diagnostics.CodeAnalysis;
using Trident.Abstractions.FileModels;

namespace Trident.Abstractions.Extensions;

public static class ProfileExtensions
{
    public static bool TryGetOverride<T>(
        this Profile profile,
        string key,
        [MaybeNullWhen(false)] out T value,
        T? defaultValue = default)
    {
        if (profile.Overrides.TryGetValue(key, out var result) && result is T rv)
        {
            value = rv;
            return true;
        }

        value = defaultValue;
        return false;
    }

    public static T? GetOverride<T>(this Profile profile, string key, T? defaultValue = default) =>
        profile.Overrides.TryGetValue(key, out var result) && result is T rv ? rv : defaultValue;
}